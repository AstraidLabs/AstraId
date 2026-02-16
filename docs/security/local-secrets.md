# Local secrets and environment configuration

Do not commit real secrets into tracked `appsettings*.json` files. Use **user-secrets** or environment variables.

## Required secret values

- AuthServer database: `ConnectionStrings__DefaultConnection`
- Redis: `Redis__ConnectionString`
- Internal token signing key: `InternalTokens__SigningKey`
- API integration keys / secrets:
  - `Auth__Introspection__ClientSecret`
  - `Api__AuthServer__ApiKey`
  - `Services__AuthServer__ApiKey`
  - `Services__Cms__ApiKey`
  - `Services__AppServer__ApiKey`

## dotnet user-secrets examples

Run from each project directory (`src/AuthServer`, `src/Api`, `src/AppServer`) as needed.

```bash
dotnet user-secrets init
```

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=astra;Username=__REPLACE_ME__;Password=__REPLACE_ME__"
dotnet user-secrets set "Redis:ConnectionString" "__REPLACE_ME__"
dotnet user-secrets set "InternalTokens:SigningKey" "__REPLACE_ME__"
dotnet user-secrets set "Auth:Introspection:ClientSecret" "__REPLACE_ME__"
dotnet user-secrets set "Api:AuthServer:ApiKey" "__REPLACE_ME__"
```

## Environment variable examples

```bash
export ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=astra;Username=__REPLACE_ME__;Password=__REPLACE_ME__"
export Redis__ConnectionString="__REPLACE_ME__"
export InternalTokens__SigningKey="__REPLACE_ME__"
export Auth__Introspection__ClientSecret="__REPLACE_ME__"
export Api__AuthServer__ApiKey="__REPLACE_ME__"
```

Use local-only overrides such as `appsettings.local.json` or `.env` files that are ignored by Git.
