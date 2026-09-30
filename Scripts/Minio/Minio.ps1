
. "$PSScriptRoot\..\var-env-setting-Dev.ps1"


function Read-MenuChoice {
    param(
        [string]$Prompt,
        [System.Collections.Specialized.OrderedDictionary]$Options
    )
    Write-Host $Prompt
    foreach ($key in $Options.Keys) {
        Write-Host "  $key - $($Options[$key])"
    }
    while ($true) {
        $choice = Read-Host "Enter choice"
        if (-not [string]::IsNullOrEmpty($choice) -and $Options.Contains($choice)) {
            return $choice
        }
        Write-Host "Invalid choice, try again."
    }
}

function Resolve-UserByNationalId {
    param([string]$NationalId)

    $escapedId = $NationalId.Replace("'", "''")
    $query = "SET QUOTED_IDENTIFIER ON;`r`nSELECT CAST([Id] AS NVARCHAR(36)) + N'|' + [FullName] FROM [Users] WHERE [NationalId] = N'$escapedId';"

    $utf8NoBom = New-Object System.Text.UTF8Encoding $false
    $tempFile = Join-Path $env:TEMP "minio-lookup-$([System.Guid]::NewGuid().ToString('N')).sql"
    [System.IO.File]::WriteAllText($tempFile, $query, $utf8NoBom)

    $containerFile = "/tmp/$([System.IO.Path]::GetFileName($tempFile))"
    docker cp $tempFile "sql1:$containerFile" | Out-Null
    Remove-Item $tempFile

    $result = docker exec sql1 /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$env:DB_PASSWORD" -C -d PMIS -h -1 -i $containerFile
    $exitCode = $LASTEXITCODE

    docker exec sql1 rm -f $containerFile 2>$null | Out-Null

    if ($exitCode -ne 0) {
        Write-Error "Lookup against sql1 failed (exit code $exitCode) - see output above."
        exit $exitCode
    }

    $line = ($result | Where-Object { $_ -and $_.Trim() -ne '' } | Select-Object -First 1)
    if ([string]::IsNullOrWhiteSpace($line)) {
        return $null
    }

    $parts = $line.Trim().Split('|', 2)
    if ($parts.Count -lt 2) {
        return $null
    }

    return @{ Id = $parts[0].Trim(); FullName = $parts[1].Trim() }
}

function Get-RequestIdsForUser {
    param([string]$UserId)

    $query = "SET QUOTED_IDENTIFIER ON;`r`nSELECT CAST([Id] AS NVARCHAR(36)) FROM [Requests] WHERE [UserId] = '$UserId';"

    $utf8NoBom = New-Object System.Text.UTF8Encoding $false
    $tempFile = Join-Path $env:TEMP "minio-requests-$([System.Guid]::NewGuid().ToString('N')).sql"
    [System.IO.File]::WriteAllText($tempFile, $query, $utf8NoBom)

    $containerFile = "/tmp/$([System.IO.Path]::GetFileName($tempFile))"
    docker cp $tempFile "sql1:$containerFile" | Out-Null
    Remove-Item $tempFile

    $result = docker exec sql1 /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$env:DB_PASSWORD" -C -d PMIS -h -1 -i $containerFile
    $exitCode = $LASTEXITCODE

    docker exec sql1 rm -f $containerFile 2>$null | Out-Null

    if ($exitCode -ne 0) {
        Write-Error "Lookup against sql1 failed (exit code $exitCode) - see output above."
        exit $exitCode
    }

    return @($result | Where-Object { $_ -and $_.Trim() -ne '' } | ForEach-Object { $_.Trim() })
}

function ConvertTo-ShSingleQuoted {
    param([string]$Value)
    return $Value.Replace("'", "'\''")
}

function Invoke-Mc {
    param(
        [Parameter(Mandatory = $true)][string]$Command,
        [Parameter(Mandatory = $true)][string]$Label
    )

    $protocol = if ($env:MinIO__UseSsl -eq 'true') { 'https' } else { 'http' }
    $endpoint = ConvertTo-ShSingleQuoted $env:MinIO__Endpoint
    $accessKey = ConvertTo-ShSingleQuoted $env:MinIO__AccessKey
    $secretKey = ConvertTo-ShSingleQuoted $env:MinIO__SecretKey

    $aliasCmd = "mc alias set local ${protocol}://$endpoint '$accessKey' '$secretKey' >/dev/null 2>&1"
    $script = "$aliasCmd && $Command"

    Write-Host "Running $Label..."
    docker run --rm --network container:minio --entrypoint sh quay.io/minio/mc -c $script
    return $LASTEXITCODE
}


function Clear-EntireBucket {
    $bucket = $env:MinIO__BucketName

    Write-Host "This will PERMANENTLY delete EVERY object in MinIO bucket '$bucket' - every request attachment (requests/) and every profile photo (users/), for every user."
    $confirm = Read-Host "Type 'yes' to continue"
    if ($confirm -ne 'yes') {
        Write-Host "Cancelled."
        return
    }

    $exitCode = Invoke-Mc -Command "mc rm --recursive --force local/$bucket" -Label "full bucket wipe of '$bucket'"
    if ($exitCode -ne 0) {
        Write-Error "Bucket wipe reported errors (exit code $exitCode) - see output above."
        exit $exitCode
    }

    Write-Host "Bucket '$bucket' is now empty."
}


function Remove-AttachmentsForUser {
    $bucket = $env:MinIO__BucketName

    $user = $null
    $nationalId = $null
    while ($true) {
        $nationalId = Read-Host "Enter the National ID to delete attachments for (or press Enter to cancel)"
        if ([string]::IsNullOrWhiteSpace($nationalId)) {
            Write-Host "Cancelled - no National ID entered."
            return
        }

        $user = Resolve-UserByNationalId -NationalId $nationalId
        if ($null -eq $user) {
            Write-Host "No user found with National ID '$nationalId' - try again."
            continue
        }

        break
    }

    Write-Host "Found user: $($user.FullName) ($nationalId) - Id $($user.Id)."

    $requestIds = Get-RequestIdsForUser -UserId $user.Id
    if ($requestIds.Count -eq 0) {
        Write-Host "This user has no requests - nothing to delete."
        return
    }

    Write-Host "This will PERMANENTLY delete every attachment object (requests/{id}/...) for $($requestIds.Count) request(s) belonging to $($user.FullName) ($nationalId). Their profile photo (if any) is NOT touched by this operation."
    $confirm = Read-Host "Type 'yes' to continue"
    if ($confirm -ne 'yes') {
        Write-Host "Cancelled."
        return
    }

    $failed = @()
    foreach ($requestId in $requestIds) {
        $exitCode = Invoke-Mc -Command "mc rm --recursive --force local/$bucket/requests/$requestId/" -Label "attachment cleanup for request $requestId"
        if ($exitCode -ne 0) {
            $failed += $requestId
        }
    }

    if ($failed.Count -gt 0) {
        Write-Warning "$($failed.Count) of $($requestIds.Count) request prefix(es) reported an error (a request with no attachments reports one too - that's expected, not a failure): $($failed -join ', ')"
    }

    Write-Host "Processed $($requestIds.Count) request(s) for $($user.FullName) ($nationalId). SQL Attachments rows are untouched - see this script's header comment."
}


$operations = [ordered]@{
    "1" = "clean entire MinIO bucket (all attachments + all profile photos)"
    "2" = "delete all attachments for a user (by National ID)"
}

$opChoice = Read-MenuChoice -Prompt "Which MinIO operation?" -Options $operations

switch ($opChoice) {
    "1" { Clear-EntireBucket }
    "2" { Remove-AttachmentsForUser }
}

Write-Host "Done."
exit 0
