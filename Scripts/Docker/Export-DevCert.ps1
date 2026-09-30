
. "$PSScriptRoot\..\var-env-setting-Dev.ps1"

if (-not (Test-Path $env:DEV_CERT_DIR)) {
    New-Item -ItemType Directory -Force -Path $env:DEV_CERT_DIR | Out-Null
}
$certPath = Join-Path $env:DEV_CERT_DIR "aspnetapp.pfx"

dotnet dev-certs https -ep $certPath -p $env:DEV_CERT_PASSWORD --trust
if ($LASTEXITCODE -ne 0) {
    Write-Error "Cannot export: 'dotnet dev-certs https' failed."
    exit 1
}

Write-Host "Dev cert exported to '$certPath' for the app container to mount."
