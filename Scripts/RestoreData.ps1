
. "$PSScriptRoot\var-env-setting-Dev.ps1"

function Read-YesNo {
    param([string]$Prompt)
    while ($true) {
        $answer = Read-Host "$Prompt (y/n)"
        if ($answer -match '^(?i)y(es)?$') { return $true }
        if ($answer -match '^(?i)n(o)?$') { return $false }
        Write-Host "Invalid choice, try again."
    }
}

function Read-ExistingPath {
    param([string]$Prompt)
    while ($true) {
        $path = Read-Host $Prompt
        if (-not [string]::IsNullOrWhiteSpace($path) -and (Test-Path $path)) {
            return (Resolve-Path $path).Path
        }
        Write-Host "Path not found, try again."
    }
}

$restoreSqlServer = Read-YesNo -Prompt "Restore SQL Server?"
$restoreMongoDb    = Read-YesNo -Prompt "Restore MongoDB?"
$restoreMinio      = Read-YesNo -Prompt "Restore MinIO?"

if (-not ($restoreSqlServer -or $restoreMongoDb -or $restoreMinio)) {
    Write-Host "Nothing selected - exiting."
    exit 0
}

if ($restoreSqlServer) {
    $sqlBakPath = Read-ExistingPath -Prompt "Path to the SQL Server .bak file"
}
if ($restoreMongoDb) {
    $mongoDumpPath = Read-ExistingPath -Prompt "Path to the MongoDB dump folder (the 'mongodb-<timestamp>' folder)"
}
if ($restoreMinio) {
    $minioBackupPath = Read-ExistingPath -Prompt "Path to the MinIO backup folder (the 'minio-<timestamp>' folder)"
}

$targets = @()
if ($restoreSqlServer) { $targets += "SQL Server (database 'PMIS')" }
if ($restoreMongoDb) { $targets += "MongoDB (database 'pmis')" }
if ($restoreMinio) { $targets += "MinIO (bucket 'pmis-attachments')" }

Write-Host ""
Write-Host "This will PERMANENTLY OVERWRITE current data for: $($targets -join ', ')"
$confirmation = Read-Host "Type 'yes' to continue"
if ($confirmation -ne 'yes') {
    Write-Host "Not confirmed - exiting without changing anything."
    exit 0
}

$failures = @()

if ($restoreSqlServer) {
    Write-Host "Restoring SQL Server (database 'PMIS') from '$sqlBakPath'..."
    $bakFileName = [System.IO.Path]::GetFileName($sqlBakPath)
    $containerBakPath = "/var/opt/mssql/data/$bakFileName"

    docker cp $sqlBakPath "sql1:$containerBakPath" | Out-Null
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Cannot copy '$sqlBakPath' into the sql1 container."
        $failures += "sqlserver"
    } else {
        docker exec sql1 /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$env:DB_PASSWORD" -C -Q `
            "ALTER DATABASE [PMIS] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; RESTORE DATABASE [PMIS] FROM DISK = N'$containerBakPath' WITH REPLACE, STATS = 10; ALTER DATABASE [PMIS] SET MULTI_USER;"
        if ($LASTEXITCODE -ne 0) {
            Write-Error "SQL Server restore failed (exit code $LASTEXITCODE) - see output above."
            $failures += "sqlserver"
        } else {
            Write-Host "SQL Server restore complete."
        }
        docker exec sql1 rm -f $containerBakPath 2>$null | Out-Null
    }
}

if ($restoreMongoDb) {
    Write-Host "Restoring MongoDB (database 'pmis') from '$mongoDumpPath'..."
    $dumpName = [System.IO.Path]::GetFileName($mongoDumpPath.TrimEnd('\', '/'))
    $containerDumpPath = "/tmp/$dumpName"

    docker cp $mongoDumpPath "mongodb:$containerDumpPath" | Out-Null
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Cannot copy '$mongoDumpPath' into the mongodb container."
        $failures += "mongodb"
    } else {
        docker exec mongodb mongorestore --uri "$env:ConnectionStrings__MongoDb" --drop --dir "$containerDumpPath" --quiet
        if ($LASTEXITCODE -ne 0) {
            Write-Error "MongoDB restore failed (exit code $LASTEXITCODE) - see output above."
            $failures += "mongodb"
        } else {
            Write-Host "MongoDB restore complete."
        }
        docker exec mongodb rm -rf $containerDumpPath 2>$null | Out-Null
    }
}

if ($restoreMinio) {
    Write-Host "Restoring MinIO (bucket 'pmis-attachments') from '$minioBackupPath'..."
    $backupName = [System.IO.Path]::GetFileName($minioBackupPath.TrimEnd('\', '/'))
    $containerBackupPath = "/tmp/$backupName"

    docker cp $minioBackupPath "minio:$containerBackupPath" | Out-Null
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Cannot copy '$minioBackupPath' into the minio container."
        $failures += "minio"
    } else {
        docker exec minio mc alias set restore-target http://localhost:9000 "$env:MINIO_ROOT_USER" "$env:MINIO_ROOT_PASSWORD" | Out-Null
        docker exec minio mc mirror --quiet --overwrite --remove $containerBackupPath restore-target/pmis-attachments
        if ($LASTEXITCODE -ne 0) {
            Write-Error "MinIO restore failed (exit code $LASTEXITCODE) - see output above."
            $failures += "minio"
        } else {
            Write-Host "MinIO restore complete."
        }
        docker exec minio rm -rf $containerBackupPath 2>$null | Out-Null
    }
}

if ($failures.Count -gt 0) {
    Write-Error "Restore completed with failures: $($failures -join ', ') - see output above."
    exit 1
}

Write-Host "Done."
exit 0
