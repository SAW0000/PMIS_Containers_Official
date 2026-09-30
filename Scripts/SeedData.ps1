
function Read-MenuChoice {
    param(
        [string]$Prompt,
        [hashtable]$Options
    )
    Write-Host $Prompt
    foreach ($key in ($Options.Keys | Sort-Object)) {
        Write-Host "  $key - $($Options[$key])"
    }
    while ($true) {
        $choice = Read-Host "Enter choice"
        if (-not [string]::IsNullOrEmpty($choice) -and $Options.ContainsKey($choice)) {
            return $choice
        }
        Write-Host "Invalid choice, try again."
    }
}

$dbChoice = Read-MenuChoice -Prompt "Which database?" -Options @{ "1" = "SQLServer"; "2" = "MongoDB" }

if ($dbChoice -eq "1") {
    & "$PSScriptRoot\SQLServer\SQL_Seed_Data.ps1"
    exit $LASTEXITCODE
}

$mongoChoice = Read-MenuChoice -Prompt "Which MongoDB tool?" -Options @{
    "1" = "seed notifications for a user by National ID (Mongo_Seed_Data.ps1)"
    "2" = "clear notifications or system logs outright (Manage-MongoData.ps1)"
}

if ($mongoChoice -eq "1") {
    & "$PSScriptRoot\MongoDB\Mongo_Seed_Data.ps1"
} else {
    & "$PSScriptRoot\MongoDB\Manage-MongoData.ps1"
}
exit $LASTEXITCODE
