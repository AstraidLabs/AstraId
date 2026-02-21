# Production-style host run (no Docker)

## One-command startup
- Bash: `./scripts/astraid-prod.sh up`
- PowerShell: `./scripts/astraid-prod.ps1 up`

`up` runs setup, DB bootstrap, migrations, seed discovery, publish, service launch, and verification.

## Publish only
- Bash: `./scripts/astraid-prod.sh publish`
- PowerShell: `./scripts/astraid-prod.ps1 publish`

Publish outputs are written to `artifacts/`.

## Stop everything
- Bash: `./scripts/astraid-prod.sh down`
- PowerShell: `./scripts/astraid-prod.ps1 down`
