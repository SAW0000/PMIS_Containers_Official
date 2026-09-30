
$ClearSystemLogsFile     = "$PSScriptRoot\mongo-clear-system-logs.js"
$ClearNotificationsFile  = "$PSScriptRoot\mongo-clear-notifications.js"

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

$operations = [ordered]@{
    "1" = "clear-system-logs"
    "2" = "clear-users-notification"
}

$opChoice = Read-MenuChoice -Prompt "Which MongoDB operation?" -Options $operations
$operationName = $operations[$opChoice]

$targetFile = switch ($opChoice) {
    "1" { $ClearSystemLogsFile }
    "2" { $ClearNotificationsFile }
}

$confirm = Read-Host "This will permanently delete ALL documents in the target collection. Type 'yes' to continue"
if ($confirm -ne "yes") {
    Write-Host "Cancelled."
    exit 0
}

if (-not (Test-Path $targetFile)) {
    Write-Error "Cannot run: '$targetFile' does not exist."
    exit 1
}
$resolvedFile = (Resolve-Path $targetFile).Path
Write-Host "Running '$operationName' ('$resolvedFile')..."

$containerFile = "/tmp/$([System.IO.Path]::GetFileName($resolvedFile))"
docker cp $resolvedFile "mongodb:$containerFile" | Out-Null

docker exec mongodb mongosh $env:ConnectionStrings__MongoDb --quiet $containerFile
$exitCode = $LASTEXITCODE

docker exec mongodb rm -f $containerFile 2>$null | Out-Null

if ($exitCode -ne 0) {
    Write-Error "Run failed (exit code $exitCode) - see output above."
    exit $exitCode
}

Write-Host "Done."
exit 0
