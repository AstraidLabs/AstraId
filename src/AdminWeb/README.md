# AuthServer Admin UI

This React + Vite admin UI is hosted by the AuthServer Razor Pages admin shell. Assets are served from
`wwwroot/admin-ui`.

## Build

From the repository root:

```bash
dotnet msbuild src/AuthServer/AuthServer.csproj /t:BuildAdminUi
```

The target will run `npm ci` when `node_modules` is missing, build the Vite app, and copy the output to
`src/AuthServer/wwwroot/admin-ui`.
