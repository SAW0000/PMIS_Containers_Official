
param(
    [Parameter(Mandatory = $true)]
    [string]$File,

    [string]$Database = "PMIS"
)

. "$PSScriptRoot\var-env-setting-Dev.ps1"

if (-not (Test-Path $File)) {
    Write-Error "Cannot run: '$File' does not exist."
    exit 1
}
$resolvedFile = (Resolve-Path $File).Path

$content = "SET QUOTED_IDENTIFIER ON;`r`n" + (Get-Content -Raw -Encoding UTF8 -Path $resolvedFile)
$utf8NoBom = New-Object System.Text.UTF8Encoding $false
$tempFile = Join-Path $env:TEMP ([System.IO.Path]::GetFileName($resolvedFile))
[System.IO.File]::WriteAllText($tempFile, $content, $utf8NoBom)

$containerFile = "/tmp/$([System.IO.Path]::GetFileName($resolvedFile))"
docker cp $tempFile "sql1:$containerFile" | Out-Null
Remove-Item $tempFile

Write-Host "Running '$resolvedFile' against database '$Database' on sql1..."
docker exec sql1 /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$env:DB_PASSWORD" -C -d $Database -i $containerFile
$exitCode = $LASTEXITCODE

docker exec sql1 rm -f $containerFile 2>$null | Out-Null

if ($exitCode -ne 0) {
    Write-Error "sqlcmd reported errors running '$resolvedFile' (exit code $exitCode) - see output above."
    exit $exitCode
}

Write-Host "Done."
exit 0
