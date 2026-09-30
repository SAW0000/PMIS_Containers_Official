
param(
    [switch]$Container,
    [switch]$Local,
    [switch]$AllInterfaces,
    [string]$BypassNafathNationalId
)
$flagCount = @($Container, $Local, $AllInterfaces) | Where-Object { $_ } | Measure-Object | Select-Object -ExpandProperty Count
if ($flagCount -gt 1) {
    Write-Error "Cannot launch: pass only one of -Container, -Local or -AllInterfaces, not several."
    exit 1
}
if ($Container) {
    $runMode = 'container'
} elseif ($Local -or $AllInterfaces) {
    $runMode = 'host'
} else {
    Write-Host "Run the app on the host or in a container?"
    Write-Host "  1: host (dotnet run)"
    Write-Host "  2: container (rebuild image, replace container)"
    do {
        $modeChoice = Read-Host "Enter 1 or 2"
    } while ($modeChoice -ne '1' -and $modeChoice -ne '2')
    $runMode = if ($modeChoice -eq '2') { 'container' } else { 'host' }
}

if ($runMode -eq 'host') {
    if ($Local) {
        $launchProfile = 'https'
    } elseif ($AllInterfaces) {
        $launchProfile = 'lan-http'
    } else {
        Write-Host "Select launch profile:"
        Write-Host "  1: https (local only)"
        Write-Host "  2: lan-http (all interfaces)"
        do {
            $choice = Read-Host "Enter 1 or 2"
        } while ($choice -ne '1' -and $choice -ne '2')
        $launchProfile = if ($choice -eq '2') { 'lan-http' } else { 'https' }
    }
}

if ($BypassNafathNationalId) {
    $env:Nafath__BypassNationalId = $BypassNafathNationalId.Trim()
} elseif ($flagCount -gt 0) {
    $env:Nafath__BypassNationalId = $null
} else {
    Write-Host "Bypass Nafath login for this launch?"
    Write-Host "  1: no (real Nafath login)"
    Write-Host "  2: yes (sign in as a National ID you supply, skipping the real Nafath round-trip)"
    do {
        $bypassChoice = Read-Host "Enter 1 or 2"
    } while ($bypassChoice -ne '1' -and $bypassChoice -ne '2')

    if ($bypassChoice -eq '2') {
        do {
            $bypassNationalId = Read-Host "National ID to bypass Nafath with"
        } while ([string]::IsNullOrWhiteSpace($bypassNationalId))
        $env:Nafath__BypassNationalId = $bypassNationalId.Trim()
    } else {
        $env:Nafath__BypassNationalId = $null
    }
}
if ($env:Nafath__BypassNationalId) {
    Write-Host "Nafath login will be bypassed for National ID '$($env:Nafath__BypassNationalId)' - if no such user exists, login will fail normally."
}

if ($runMode -eq 'container') {
    Write-Host "Skipping host-process check ('PMIS' process) - container mode does not run the app on the host."
} else {
    $existing = Get-Process -Name 'PMIS' -ErrorAction SilentlyContinue
    if ($existing) {
        foreach ($proc in $existing) {
            Write-Host "Stopping existing PMIS process (PID $($proc.Id))..."
            Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
        }
        $maxWaitSeconds = 15
        $elapsed = 0
        while ((Get-Process -Name 'PMIS' -ErrorAction SilentlyContinue) -and $elapsed -lt $maxWaitSeconds) {
            Start-Sleep -Milliseconds 500
            $elapsed += 0.5
        }
        if (Get-Process -Name 'PMIS' -ErrorAction SilentlyContinue) {
            Write-Error "Cannot launch: an existing PMIS process did not exit after $maxWaitSeconds s - close it manually and retry."
            exit 1
        }
        Write-Host "Existing PMIS process stopped."
    }
}

. "$PSScriptRoot\var-env-setting-Dev.ps1"

docker compose up -d
if ($LASTEXITCODE -ne 0) {
    Write-Error "Cannot launch: 'docker compose up -d' failed - is Docker running?"
    exit 1
}

Write-Host "Waiting for mongodb container to accept authenticated connections..."
$maxWaitSeconds = 60
$elapsed = 0
$mongoReady = $false
while ($elapsed -lt $maxWaitSeconds) {
    docker exec mongodb mongosh $env:ConnectionStrings__MongoDb --quiet --eval "db.getSiblingDB('pmis').getCollectionNames()" *> $null
    if ($LASTEXITCODE -eq 0) { $mongoReady = $true; break }
    Start-Sleep -Seconds 2
    $elapsed += 2
}
if (-not $mongoReady) {
    Write-Error "Cannot launch: mongodb container did not accept an authenticated connection after $maxWaitSeconds s."
    exit 1
}

Write-Host "Applying MongoDB structure migration..."
docker cp "$PSScriptRoot\MongoDB\mongo-init.js" mongodb:/tmp/mongo-init.js | Out-Null
docker exec mongodb mongosh $env:ConnectionStrings__MongoDb --quiet /tmp/mongo-init.js
docker exec mongodb rm -f /tmp/mongo-init.js
if ($LASTEXITCODE -ne 0) {
    Write-Error "Cannot launch: MongoDB structure migration (mongo-init.js) failed."
    exit 1
}

Write-Host "Waiting for sqlserver container to report healthy..."
$maxWaitSeconds = 60
$elapsed = 0
while ($elapsed -lt $maxWaitSeconds) {
    $status = docker inspect --format='{{.State.Health.Status}}' sql1 2>$null
    if ($status -eq 'healthy') { break }
    Start-Sleep -Seconds 2
    $elapsed += 2
}
if ($status -ne 'healthy') {
    Write-Error "Cannot launch: sqlserver container did not report healthy after $maxWaitSeconds s (last status: $status)."
    exit 1
}

Write-Host "Restoring NuGet packages..."
dotnet restore "$PSScriptRoot\..\PMIS.csproj"
if ($LASTEXITCODE -ne 0) {
    Write-Error "Cannot launch: 'dotnet restore' failed."
    exit 1
}

Write-Host "Applying SQL Server migrations..."
dotnet ef database update --project "$PSScriptRoot\.."
if ($LASTEXITCODE -ne 0) {
    Write-Error "Cannot launch: 'dotnet ef database update' failed - check the migration output above."
    exit 1
}

Write-Host "Waiting for minio container to report healthy..."
$maxWaitSeconds = 60
$elapsed = 0
while ($elapsed -lt $maxWaitSeconds) {
    $status = docker inspect --format='{{.State.Health.Status}}' minio 2>$null
    if ($status -eq 'healthy') { break }
    Start-Sleep -Seconds 2
    $elapsed += 2
}
if ($status -ne 'healthy') {
    Write-Error "Cannot launch: minio container did not report healthy after $maxWaitSeconds s (last status: $status)."
    exit 1
}

Write-Host "Waiting for redis container to report healthy..."
$maxWaitSeconds = 60
$elapsed = 0
while ($elapsed -lt $maxWaitSeconds) {
    $status = docker inspect --format='{{.State.Health.Status}}' redis 2>$null
    if ($status -eq 'healthy') { break }
    Start-Sleep -Seconds 2
    $elapsed += 2
}
if ($status -ne 'healthy') {
    Write-Error "Cannot launch: redis container did not report healthy after $maxWaitSeconds s (last status: $status)."
    exit 1
}

Write-Host "Waiting for clamav container to report healthy..."
$maxWaitSeconds = 180
$elapsed = 0
while ($elapsed -lt $maxWaitSeconds) {
    $status = docker inspect --format='{{.State.Health.Status}}' clamav 2>$null
    if ($status -eq 'healthy') { break }
    Start-Sleep -Seconds 2
    $elapsed += 2
}
if ($status -ne 'healthy') {
    Write-Error "Cannot launch: clamav container did not report healthy after $maxWaitSeconds s (last status: $status)."
    exit 1
}

if ($runMode -eq 'host') {
    dotnet run --project "$PSScriptRoot\.." --launch-profile $launchProfile
    exit $LASTEXITCODE
}

$composeAppArgs = @('-f', 'docker-compose.yml', '-f', 'docker-compose.app.yml')
$repoRoot = "$PSScriptRoot\.."

$certPath = Join-Path $env:DEV_CERT_DIR "aspnetapp.pfx"
if (-not (Test-Path $certPath)) {
    Write-Host "Dev cert not found at '$certPath' - exporting it now..."
    & "$PSScriptRoot\Docker\Export-DevCert.ps1"
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Cannot launch: Export-DevCert.ps1 failed."
        exit 1
    }
}

Push-Location $repoRoot
try {
    Write-Host "Removing any existing 'pmis-app' container..."
    docker compose @composeAppArgs rm -f -s app | Out-Null

    Write-Host "Building image and starting 'pmis-app' container..."
    docker compose @composeAppArgs up -d --build app
    $composeExitCode = $LASTEXITCODE
} finally {
    Pop-Location
}
if ($composeExitCode -ne 0) {
    Write-Error "Cannot launch: 'docker compose up -d --build app' failed - see output above."
    exit 1
}

Write-Host "pmis-app container is up - http://localhost:$($env:APP_HTTP_PORT), https://localhost:$($env:APP_HTTPS_PORT)"
