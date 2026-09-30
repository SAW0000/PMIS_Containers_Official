
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

$backupSqlServer = Read-YesNo -Prompt "Back up SQL Server?"
$backupMongoDb    = Read-YesNo -Prompt "Back up MongoDB?"
$backupMinio      = Read-YesNo -Prompt "Back up MinIO?"

if (-not ($backupSqlServer -or $backupMongoDb -or $backupMinio)) {
    Write-Host "Nothing selected - exiting."
    exit 0
}

do {
    $destRoot = Read-Host "Path to save the backup(s) to"
} while ([string]::IsNullOrWhiteSpace($destRoot))

$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$destDir = Join-Path $destRoot "pmis-backup-$timestamp"
New-Item -ItemType Directory -Force -Path $destDir | Out-Null
Write-Host "Saving backup(s) to '$destDir'..."

$failures = @()

if ($backupSqlServer) {
    Write-Host "Backing up SQL Server (database 'PMIS')..."
    $bakFileName = "PMIS-$timestamp.bak"
    $containerBakPath = "/var/opt/mssql/data/$bakFileName"

    docker exec sql1 /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$env:DB_PASSWORD" -C -Q `
        "BACKUP DATABASE [PMIS] TO DISK = N'$containerBakPath' WITH INIT, STATS = 10;"
    if ($LASTEXITCODE -ne 0) {
        Write-Error "SQL Server backup failed (exit code $LASTEXITCODE) - see output above."
        $failures += "sqlserver"
    } else {
        docker cp "sql1:$containerBakPath" (Join-Path $destDir $bakFileName) | Out-Null
        docker exec sql1 rm -f $containerBakPath 2>$null | Out-Null
        if ($LASTEXITCODE -ne 0 -and -not (Test-Path (Join-Path $destDir $bakFileName))) {
            Write-Error "Cannot copy SQL Server backup out of the container."
            $failures += "sqlserver"
        } else {
            Write-Host "SQL Server backup saved to '$(Join-Path $destDir $bakFileName)'."
        }
    }
}

if ($backupMongoDb) {
    Write-Host "Backing up MongoDB (database 'pmis')..."
    $mongoDumpName = "mongodb-$timestamp"
    $containerDumpPath = "/tmp/$mongoDumpName"

    docker exec mongodb mongodump --uri "$env:ConnectionStrings__MongoDb" --db pmis --out $containerDumpPath --quiet
    if ($LASTEXITCODE -ne 0) {
        Write-Error "MongoDB backup failed (exit code $LASTEXITCODE) - see output above."
        $failures += "mongodb"
    } else {
        docker cp "mongodb:$containerDumpPath" (Join-Path $destDir $mongoDumpName) | Out-Null
        $copyExitCode = $LASTEXITCODE
        docker exec mongodb rm -rf $containerDumpPath 2>$null | Out-Null
        if ($copyExitCode -ne 0 -and -not (Test-Path (Join-Path $destDir $mongoDumpName))) {
            Write-Error "Cannot copy MongoDB dump out of the container."
            $failures += "mongodb"
        } else {
            Write-Host "MongoDB dump saved to '$(Join-Path $destDir $mongoDumpName)'."
        }
    }
}

if ($backupMinio) {
    Write-Host "Backing up MinIO (bucket 'pmis-attachments')..."
    $minioBackupName = "minio-$timestamp"
    $containerBackupPath = "/tmp/$minioBackupName"

    docker exec minio mc alias set backup-source http://localhost:9000 "$env:MINIO_ROOT_USER" "$env:MINIO_ROOT_PASSWORD" | Out-Null
    docker exec minio mc mirror --quiet backup-source/pmis-attachments $containerBackupPath
    if ($LASTEXITCODE -ne 0) {
        Write-Error "MinIO backup failed (exit code $LASTEXITCODE) - see output above."
        $failures += "minio"
    } else {
        docker cp "minio:$containerBackupPath" (Join-Path $destDir $minioBackupName) | Out-Null
        $copyExitCode = $LASTEXITCODE
        docker exec minio rm -rf $containerBackupPath 2>$null | Out-Null
        if ($copyExitCode -ne 0 -and -not (Test-Path (Join-Path $destDir $minioBackupName))) {
            Write-Error "Cannot copy MinIO backup out of the container."
            $failures += "minio"
        } else {
            Write-Host "MinIO backup saved to '$(Join-Path $destDir $minioBackupName)'."
        }
    }
}

if ($failures.Count -gt 0) {
    Write-Error "Backup completed with failures: $($failures -join ', ') - see output above."
    exit 1
}

Write-Host "Done. Backup(s) saved to '$destDir'."
exit 0
