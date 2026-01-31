$Root = Resolve-Path (Join-Path $PSScriptRoot "..")

Write-Host "Starting AuthServer on https://localhost:7001"
$auth = Start-Process -FilePath "dotnet" -ArgumentList "run", "--project", "$Root/src/AuthServer", "--launch-profile", "AuthServer" -PassThru

Write-Host "Starting Api on https://localhost:7002"
$api = Start-Process -FilePath "dotnet" -ArgumentList "run", "--project", "$Root/src/Api", "--launch-profile", "Api" -PassThru

Write-Host "Starting Web on http://localhost:5173"
Set-Location "$Root/src/Web"
if (-not (Test-Path "node_modules")) {
  npm install
}

npm run dev

Write-Host "Stopping services..."
if ($auth) { Stop-Process -Id $auth.Id -ErrorAction SilentlyContinue }
if ($api) { Stop-Process -Id $api.Id -ErrorAction SilentlyContinue }
