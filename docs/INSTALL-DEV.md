# Development install and run

## One-command startup
- Bash: `./scripts/astraid-dev.sh up`
- PowerShell: `./scripts/astraid-dev.ps1 up`

This runs: `setup -> db -> migrate -> seed -> up -> verify`.

## Services started in separate terminals
- AuthServer (`https://localhost:7001`)
- Api (`https://localhost:7002`)
- AppServer (`https://localhost:7003`)
- Web/Vite (`http://localhost:5173`)

## Stop everything
- Bash: `./scripts/astraid-dev.sh down`
- PowerShell: `./scripts/astraid-dev.ps1 down`
