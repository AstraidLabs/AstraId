# Nastavení e-mailů (AuthServer)

## Výchozí konfigurace

- **Development** posílá e-maily do smtp4dev:
  - SMTP host: `localhost`
  - SMTP port: `2525`
  - UI: např. `http://localhost:5000`
- Konfigurace je v `appsettings.Development.json` v sekci `Email`.

## Produkční nastavení

V produkci doplňte SMTP přihlašovací údaje přes secrets nebo proměnné prostředí:

```
dotnet user-secrets set "Email:Smtp:Password" "your-secret-password"
dotnet user-secrets set "Email:Smtp:Username" "your-smtp-user"
dotnet user-secrets set "Email:Smtp:Host" "smtp.your-provider.com"
dotnet user-secrets set "Email:Smtp:Port" "587"
dotnet user-secrets set "Email:FromEmail" "no-reply@your-domain.com"
dotnet user-secrets set "Email:FromName" "AstraId"
```

> Poznámka: `Email:Smtp:Password` není uložen v `appsettings.json` z bezpečnostních důvodů.
