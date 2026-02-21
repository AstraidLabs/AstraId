param([Parameter(Position=0)][string]$Command)
$ErrorActionPreference = 'Stop'
. "$PSScriptRoot/common/common.ps1"
$envFile = Join-Path $Script:RootDir '.env.prod'

function Setup { Require-Command dotnet; Ensure-EnvFromExample (Join-Path $Script:RootDir '.env') (Join-Path $Script:RootDir '.env.example'); Ensure-EnvFromExample $envFile (Join-Path $Script:RootDir '.env.prod.example'); New-Item -ItemType Directory -Path (Join-Path $Script:RootDir 'artifacts') -Force | Out-Null }
function Db { & "$PSScriptRoot/astraid-dev.ps1" db }
function Migrate { & "$PSScriptRoot/astraid-dev.ps1" migrate }
function Seed { & "$PSScriptRoot/astraid-dev.ps1" seed }

function Publish {
  foreach ($svc in @('AuthServer','Api','AppServer')) {
    $proj = Join-Path $Script:RootDir "src/$svc/$svc.csproj"
    if (Test-Path $proj) { dotnet publish $proj -c Release -o (Join-Path $Script:RootDir "artifacts/$svc") }
  }
  $webPath = Join-Path $Script:RootDir 'src/Web'
  if (Test-Path (Join-Path $webPath 'package.json')) { Push-Location $webPath; if (-not (Test-Path node_modules)) { npm ci }; npm run build; Pop-Location }
}

function Verify {
  $env:AUTHSERVER_ISSUER = if ($env:AUTHSERVER_ISSUER) { $env:AUTHSERVER_ISSUER } else { 'https://localhost:7001' }
  $env:API_BASE_URL = if ($env:API_BASE_URL) { $env:API_BASE_URL } else { 'https://localhost:7002' }
  $env:APPSERVER_BASE_URL = if ($env:APPSERVER_BASE_URL) { $env:APPSERVER_BASE_URL } else { 'https://localhost:7003' }
  $env:WEB_BASE_URL = if ($env:WEB_BASE_URL) { $env:WEB_BASE_URL } else { 'http://localhost:4173' }
  & "$PSScriptRoot/astraid-dev.ps1" verify
}

function Up {
  Setup; Db; Migrate; Seed; Publish
  $runner = New-RunnerScript 'AuthServer-prod' $envFile $Script:RootDir '$env:ASPNETCORE_ENVIRONMENT="Production"; dotnet artifacts/AuthServer/AuthServer.dll' (Join-Path $Script:StateDir 'prod-auth.pid')
  Start-InTerminal 'AstraId AuthServer (prod)' $runner
  if (Test-Path (Join-Path $Script:RootDir 'artifacts/Api/Api.dll')) { $runner = New-RunnerScript 'Api-prod' $envFile $Script:RootDir '$env:ASPNETCORE_ENVIRONMENT="Production"; dotnet artifacts/Api/Api.dll' (Join-Path $Script:StateDir 'prod-api.pid'); Start-InTerminal 'AstraId Api (prod)' $runner }
  if (Test-Path (Join-Path $Script:RootDir 'artifacts/AppServer/AppServer.dll')) { $runner = New-RunnerScript 'AppServer-prod' $envFile $Script:RootDir '$env:ASPNETCORE_ENVIRONMENT="Production"; dotnet artifacts/AppServer/AppServer.dll' (Join-Path $Script:StateDir 'prod-app.pid'); Start-InTerminal 'AstraId AppServer (prod)' $runner }
  if (Test-Path (Join-Path $Script:RootDir 'src/Web/package.json')) { $runner = New-RunnerScript 'Web-prod' $envFile (Join-Path $Script:RootDir 'src/Web') 'npm run preview -- --host 0.0.0.0 --port 4173' (Join-Path $Script:StateDir 'prod-web.pid'); Start-InTerminal 'AstraId Web (prod preview)' $runner }
  Start-Sleep -Seconds 4
  Verify
}

function Down { Stop-PidFile (Join-Path $Script:StateDir 'prod-auth.pid'); Stop-PidFile (Join-Path $Script:StateDir 'prod-api.pid'); Stop-PidFile (Join-Path $Script:StateDir 'prod-app.pid'); Stop-PidFile (Join-Path $Script:StateDir 'prod-web.pid') }

switch ($Command) {
  'setup' { Setup }
  'db' { Db }
  'migrate' { Migrate }
  'seed' { Seed }
  'publish' { Publish }
  'up' { Up }
  'down' { Down }
  'verify' { Verify }
  default { throw 'Usage: ./scripts/astraid-prod.ps1 {setup|db|migrate|seed|publish|up|down|verify}' }
}
