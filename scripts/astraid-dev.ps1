param([Parameter(Position=0)][string]$Command)
$ErrorActionPreference = 'Stop'
. "$PSScriptRoot/common/common.ps1"
$envFile = Join-Path $Script:RootDir ".env.dev"

function Setup {
  Require-Command dotnet
  Require-Command curl
  Ensure-EnvFromExample (Join-Path $Script:RootDir '.env') (Join-Path $Script:RootDir '.env.example')
  Ensure-EnvFromExample $envFile (Join-Path $Script:RootDir '.env.dev.example')
  New-Item -ItemType Directory -Path (Join-Path $Script:RootDir '.keys') -Force | Out-Null
  New-Item -ItemType Directory -Path (Join-Path $Script:RootDir '.certs') -Force | Out-Null
  New-Item -ItemType Directory -Path (Join-Path $Script:RootDir 'artifacts') -Force | Out-Null
  $webPath = Join-Path $Script:RootDir 'src/Web'
  if ((Test-Path $webPath) -and -not (Test-Path (Join-Path $webPath 'node_modules'))) { Push-Location $webPath; npm ci; Pop-Location }
}

function Db {
  if (-not (Get-Command psql -ErrorAction SilentlyContinue)) { Write-Warn 'psql missing. Run db/*.sql manually.'; return }
  Import-EnvFile $envFile
  $hostName = if ($env:PGHOST) { $env:PGHOST } else { 'localhost' }
  $port = if ($env:PGPORT) { $env:PGPORT } else { '5432' }
  $db = if ($env:PGADMIN_DB) { $env:PGADMIN_DB } else { 'postgres' }
  $user = if ($env:PGSUPERUSER) { $env:PGSUPERUSER } else { 'postgres' }
  Get-ChildItem (Join-Path $Script:RootDir 'db/*.sql') | Sort-Object Name | ForEach-Object {
    psql "host=$hostName port=$port dbname=$db user=$user" -v ON_ERROR_STOP=1 -f $_.FullName
  }
}

function Migrate {
  if (-not (Get-Command dotnet-ef -ErrorAction SilentlyContinue)) { Write-Warn 'dotnet-ef missing.'; return }
  Get-ChildItem (Join-Path $Script:RootDir 'src') -Recurse -Filter *.csproj | ForEach-Object {
    $dir = Split-Path $_.FullName -Parent
    if (Test-Path (Join-Path $dir 'Migrations')) { dotnet ef database update --project $_.FullName --startup-project $_.FullName }
  }
}

function Seed {
  Write-Log 'Seeding is handled by AuthBootstrapHostedService during AuthServer startup.'
}

function Verify {
  Import-EnvFile $envFile
  $issuer = if ($env:AUTHSERVER_ISSUER) { $env:AUTHSERVER_ISSUER.TrimEnd('/') } else { 'https://localhost:7001' }
  $api = if ($env:API_BASE_URL) { $env:API_BASE_URL } else { 'https://localhost:7002' }
  $app = if ($env:APPSERVER_BASE_URL) { $env:APPSERVER_BASE_URL } else { 'https://localhost:7003' }
  $web = if ($env:WEB_BASE_URL) { $env:WEB_BASE_URL } else { 'http://localhost:5173' }
  Write-Host "Issuer discovery: $issuer/.well-known/openid-configuration"
  try { curl -kfsS "$issuer/.well-known/openid-configuration" | Out-Null; Write-Host '  OK' } catch { Write-Host '  FAILED' }
  Write-Host "Api health: $api/health"; try { curl -kfsS "$api/health" | Out-Null; Write-Host '  OK' } catch { Write-Host '  FAILED' }
  Write-Host "AppServer health: $app/health"; try { curl -kfsS "$app/health" | Out-Null; Write-Host '  OK' } catch { Write-Host '  FAILED' }
  Write-Host "Web: $web"
}

function Up {
  Setup; Db; Migrate; Seed
  $runner = New-RunnerScript 'AuthServer-dev' $envFile $Script:RootDir "dotnet watch run --project src/AuthServer --launch-profile AuthServer" (Join-Path $Script:StateDir 'dev-auth.pid')
  Start-InTerminal 'AstraId AuthServer (dev)' $runner
  if (Test-Path (Join-Path $Script:RootDir 'src/Api/Api.csproj')) { $runner = New-RunnerScript 'Api-dev' $envFile $Script:RootDir "dotnet watch run --project src/Api --launch-profile Api" (Join-Path $Script:StateDir 'dev-api.pid'); Start-InTerminal 'AstraId Api (dev)' $runner }
  if (Test-Path (Join-Path $Script:RootDir 'src/AppServer/AppServer.csproj')) { $runner = New-RunnerScript 'AppServer-dev' $envFile $Script:RootDir "dotnet watch run --project src/AppServer --launch-profile AppServer" (Join-Path $Script:StateDir 'dev-app.pid'); Start-InTerminal 'AstraId AppServer (dev)' $runner }
  if (Test-Path (Join-Path $Script:RootDir 'src/Web/package.json')) { $runner = New-RunnerScript 'Web-dev' $envFile (Join-Path $Script:RootDir 'src/Web') "if (-not (Test-Path node_modules)) { npm ci }; npm run dev" (Join-Path $Script:StateDir 'dev-web.pid'); Start-InTerminal 'AstraId Web (dev)' $runner }
  Start-Sleep -Seconds 4
  Verify
}

function Down {
  Stop-PidFile (Join-Path $Script:StateDir 'dev-auth.pid')
  Stop-PidFile (Join-Path $Script:StateDir 'dev-api.pid')
  Stop-PidFile (Join-Path $Script:StateDir 'dev-app.pid')
  Stop-PidFile (Join-Path $Script:StateDir 'dev-web.pid')
}

switch ($Command) {
  'setup' { Setup }
  'db' { Db }
  'migrate' { Migrate }
  'seed' { Seed }
  'up' { Up }
  'down' { Down }
  'verify' { Verify }
  default { throw 'Usage: ./scripts/astraid-dev.ps1 {setup|db|migrate|seed|up|down|verify}' }
}
