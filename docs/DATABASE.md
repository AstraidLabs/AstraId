# Database bootstrap

SQL bootstrap scripts are in `db/` and are idempotent:
1. `00-create-role.sql`
2. `01-create-db.sql`
3. `02-extensions.sql`
4. `03-schema.sql`
5. `04-privileges.sql`

Run through helper command:
- Bash: `./scripts/astraid-dev.sh db`
- PowerShell: `./scripts/astraid-dev.ps1 db`

If `psql` is unavailable, scripts print manual steps.
