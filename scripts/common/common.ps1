Set-StrictMode -Version Latest
$Script:RootDir = (Resolve-Path (Join-Path $PSScriptRoot "../..")).Path
$Script:StateDir = Join-Path $Script:RootDir "scripts/.state"
New-Item -ItemType Directory -Path $Script:StateDir -Force | Out-Null

function Write-Log([string]$Message){ Write-Host "[astraid] $Message" }
function Write-Warn([string]$Message){ Write-Warning "[astraid] $Message" }

function Ensure-EnvFromExample([string]$Target,[string]$Example){ if (-not (Test-Path $Target) -and (Test-Path $Example)) { Copy-Item $Example $Target; Write-Log "Created $(Split-Path $Target -Leaf)" } }
function Require-Command([string]$Name){ if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) { throw "Missing dependency: $Name" } }

function Import-EnvFile([string]$Path){
  if (-not (Test-Path $Path)) { return }
  Get-Content $Path | ForEach-Object {
    if ($_ -match '^\s*#' -or $_ -notmatch '=') { return }
    $parts = $_.Split('=',2)
    [Environment]::SetEnvironmentVariable($parts[0], $parts[1])
  }
}

function New-RunnerScript([string]$Name,[string]$EnvFile,[string]$Workdir,[string]$Command,[string]$PidFile){
  $runner = Join-Path $Script:StateDir "run-$Name.ps1"
  @"
`$ErrorActionPreference = 'Stop'
Set-Location '$Workdir'
if (Test-Path '$EnvFile') {
  Get-Content '$EnvFile' | ForEach-Object {
    if (`$_ -match '^\s*#' -or `$_ -notmatch '=') { return }
    `$p = `$_ .Split('=',2)
    [Environment]::SetEnvironmentVariable(`$p[0], `$p[1])
  }
}
`$PID | Out-File -FilePath '$PidFile' -Encoding utf8 -Force
Write-Host '[$Name] starting: $Command'
Invoke-Expression '$Command'
"@ | Set-Content -Path $runner -Encoding UTF8
  return $runner
}

function Start-InTerminal([string]$Title,[string]$RunnerPath){
  if ($IsWindows) {
    if (Get-Command wt -ErrorAction SilentlyContinue) {
      Start-Process wt -ArgumentList "new-tab","--title",$Title,"pwsh","-NoExit","-ExecutionPolicy","Bypass","-File",$RunnerPath | Out-Null
    } else {
      Start-Process powershell -ArgumentList "-NoExit","-ExecutionPolicy","Bypass","-File",$RunnerPath -WindowStyle Normal | Out-Null
    }
    return
  }
  if ($IsMacOS) {
    $escaped = $RunnerPath.Replace("'","\\'")
    osascript -e "tell application \"Terminal\" to do script \"pwsh -NoExit -File '$escaped'\"" | Out-Null
    return
  }

  if (Get-Command gnome-terminal -ErrorAction SilentlyContinue) {
    Start-Process gnome-terminal -ArgumentList "--title=$Title","--","pwsh","-NoExit","-File",$RunnerPath | Out-Null
  } elseif (Get-Command konsole -ErrorAction SilentlyContinue) {
    Start-Process konsole -ArgumentList "--new-tab","-p","tabtitle=$Title","-e","pwsh","-NoExit","-File",$RunnerPath | Out-Null
  } elseif (Get-Command xterm -ErrorAction SilentlyContinue) {
    Start-Process xterm -ArgumentList "-T",$Title,"-e","pwsh -NoExit -File '$RunnerPath'" | Out-Null
  } else {
    Write-Warn "No terminal app found; starting in current session background."
    Start-Process pwsh -ArgumentList "-NoExit","-File",$RunnerPath | Out-Null
  }
}

function Stop-PidFile([string]$PidFile){
  if (-not (Test-Path $PidFile)) { return }
  $pid = (Get-Content $PidFile -ErrorAction SilentlyContinue | Select-Object -First 1)
  if ($pid) { Stop-Process -Id ([int]$pid) -Force -ErrorAction SilentlyContinue }
  Remove-Item $PidFile -Force -ErrorAction SilentlyContinue
}
