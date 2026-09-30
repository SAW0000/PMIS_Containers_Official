param(
    [switch]$SkipMigrations
)

$prodEnvFile = "$PSScriptRoot\var-env-setting.Production.ps1"
if (-not (Test-Path $prodEnvFile)) {
    Write-Error "Cannot launch: '$prodEnvFile' does not exist - copy var-env-setting-Dev.ps1's shape and fill in real production values first."
    exit 1
}
. $prodEnvFile

$unresolvedPlaceholders = Get-ChildItem Env: | Where-Object { $_.Value -like 'CHANGE_ME_*' }
if ($unresolvedPlaceholders) {
    Write-Error "Cannot launch: var-env-setting.Production.ps1 still has placeholder values for: $($unresolvedPlaceholders.Name -join ', ')"
    exit 1
}

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

if ($SkipMigrations) {
    Write-Host "Skipping 'dotnet ef database update' (-SkipMigrations passed) - confirm migrations were already applied."
} else {
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

$composeProdArgs = @('-f', 'docker-compose.yml', '-f', 'docker-compose.prod.yml')
$repoRoot = "$PSScriptRoot\.."

Push-Location $repoRoot
try {
    Write-Host "Removing any existing 'pmis-app' container..."
    docker compose @composeProdArgs rm -f -s app | Out-Null

    Write-Host "Building image and starting 'pmis-app' and 'caddy' containers..."
    docker compose @composeProdArgs up -d --build app caddy
    $composeExitCode = $LASTEXITCODE
} finally {
    Pop-Location
}
if ($composeExitCode -ne 0) {
    Write-Error "Cannot launch: 'docker compose up -d --build app caddy' failed - see output above."
    exit 1
}

Write-Host "Waiting for caddy container to report healthy..."
$maxWaitSeconds = 60
$elapsed = 0
while ($elapsed -lt $maxWaitSeconds) {
    $status = docker inspect --format='{{.State.Health.Status}}' caddy 2>$null
    if ($status -eq 'healthy') { break }
    Start-Sleep -Seconds 2
    $elapsed += 2
}
if ($status -ne 'healthy') {
    Write-Error "Cannot launch: caddy container did not report healthy after $maxWaitSeconds s (last status: $status) - check 'docker logs caddy'."
    exit 1
}

Write-Host "pmis-app is up, reachable only through the caddy reverse proxy at https://localhost (self-signed cert - see Caddy/Caddyfile)."
