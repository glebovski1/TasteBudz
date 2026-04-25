# TasteBudz Azure App Service Deployment Commands

Use these commands from the repository root. Replace placeholder values before running.

## Update Existing App

For code-only updates to the current production App Service, use the repo-local update script instead of repeating the full resource creation flow:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .agents\skills\azure-app-service-deployment\scripts\update-published-app.ps1
```

The script defaults to:

- resource group `rg-tastebudz-prod`
- web app `tastebudz-prod-23df46c9`
- MVC host project `src/TasteBudz.Web.Mvc/TasteBudz.Web.Mvc.csproj`
- publish root `artifacts\publish`

Useful options:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .agents\skills\azure-app-service-deployment\scripts\update-published-app.ps1 -DryRun
powershell -NoProfile -ExecutionPolicy Bypass -File .agents\skills\azure-app-service-deployment\scripts\update-published-app.ps1 -Subscription "<subscription name or id>"
powershell -NoProfile -ExecutionPolicy Bypass -File .agents\skills\azure-app-service-deployment\scripts\update-published-app.ps1 -KeepArtifacts
```

The script always runs Release restore, build, and tests before publishing only the MVC host. It deploys a zip package with `az webapp deploy` first, falls back to Kudu zipdeploy when needed, restores original SCM/FTP basic publishing policy states, and verifies:

- homepage returns `200`
- unauthenticated `/api/v1/restaurants` returns `401`
- unauthenticated `POST /hubs/chat/negotiate?negotiateVersion=1` returns `401`

Generated publish artifacts are deleted unless `-KeepArtifacts` is passed.

Schema changes remain manual. When production schema changes, apply the ordered scripts from `src/TasteBudz.Database/sqlserver` as an explicit release step; do not rely on the app update script to apply SQL.

## Reliable Database + App Release

For releases that include SQL schema patches, data migrations, or higher rollback risk, use the reliable release script:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .agents\skills\azure-app-service-deployment\scripts\release-with-rollback.ps1 `
  -ScriptPath .\src\TasteBudz.Database\sqlserver\patches\20260425_example.sql `
  -DatabaseRollbackScriptPath .\src\TasteBudz.Database\sqlserver\patches\rollback\20260425_example_rollback.sql `
  -AllowClientIp
```

Dry run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .agents\skills\azure-app-service-deployment\scripts\release-with-rollback.ps1 `
  -ScriptPath .\src\TasteBudz.Database\sqlserver\patches\20260425_example.sql `
  -DatabaseRollbackScriptPath .\src\TasteBudz.Database\sqlserver\patches\rollback\20260425_example_rollback.sql `
  -DryRun
```

Behavior:

- runs Release restore, build, tests, and `git diff --check`
- downloads the current Kudu `site/wwwroot` package as a rollback snapshot before changes
- validates that the rollback snapshot and new zip package are non-empty and contain expected app files
- blocks publish output that includes environment-specific appsettings or local secret/profile files
- applies SQL scripts through the Azure SQL schema skill script
- publishes and deploys the MVC host package
- waits for synchronous Kudu zipdeploy completion when using the fallback deploy path or app rollback path
- verifies homepage `200`, unauthenticated restaurants API `401`, and unauthenticated SignalR negotiate `401`
- runs `-PostDeployVerificationScript` when supplied, with `TASTEBUDZ_BASE_URL`, `TASTEBUDZ_RESOURCE_GROUP`, and `TASTEBUDZ_WEB_APP_NAME` set for that process
- on failure, applies rollback SQL scripts when supplied and redeploys the previous app snapshot
- keeps generated artifacts after a failed release for diagnosis or manual recovery

Database rollback limits:

- App package rollback is automatic when the pre-release Kudu snapshot is captured successfully.
- Database rollback is automatic only when `-DatabaseRollbackScriptPath` is supplied.
- If a migration is intentionally forward-only, pass `-AllowForwardOnlyDatabaseChange`; the script will permit the release but can only roll back the app package if verification fails.
- For destructive or hard-to-reverse migrations, create an Azure SQL database copy or backup before release and confirm any Azure cost impact before doing so.

## Authenticate

```powershell
az login
az account list --output table
az account set --subscription "<subscription name or id>"
az account show --output table
```

Use device-code login if browser login fails:

```powershell
az login --use-device-code
```

## Variables

```powershell
$rg = "rg-tastebudz-prod"
$loc = "eastus"
$plan = "asp-tastebudz-prod"
$app = "tastebudz-prod-<unique-suffix>"
$sqlServer = "tastebudz-sql-<unique-suffix>"
$sqlDb = "TasteBudz"
$sqlAdmin = "tastebudzadmin"
$sqlPassword = "<strong-sql-password>"
```

## Create Resources

```powershell
az group create --name $rg --location $loc

az appservice plan create `
  --name $plan `
  --resource-group $rg `
  --location $loc `
  --sku B1

az webapp create `
  --resource-group $rg `
  --plan $plan `
  --name $app `
  --https-only true

az sql server create `
  --name $sqlServer `
  --resource-group $rg `
  --location $loc `
  --admin-user $sqlAdmin `
  --admin-password $sqlPassword `
  --minimal-tls-version 1.2

az sql db create `
  --resource-group $rg `
  --server $sqlServer `
  --name $sqlDb `
  --service-objective Basic
```

## Configure SQL Firewall

Allow App Service access to Azure SQL:

```powershell
az sql server firewall-rule create `
  --resource-group $rg `
  --server $sqlServer `
  --name AllowAzureServices `
  --start-ip-address 0.0.0.0 `
  --end-ip-address 0.0.0.0
```

Also add the current client IP before running local `sqlcmd` scripts:

```powershell
$clientIp = (Invoke-RestMethod "https://api.ipify.org")

az sql server firewall-rule create `
  --resource-group $rg `
  --server $sqlServer `
  --name AllowLocalClient `
  --start-ip-address $clientIp `
  --end-ip-address $clientIp
```

## Configure App Service

```powershell
$conn = "Server=tcp:$sqlServer.database.windows.net,1433;Initial Catalog=$sqlDb;Persist Security Info=False;User ID=$sqlAdmin;Password=$sqlPassword;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"

az webapp config appsettings set `
  --resource-group $rg `
  --name $app `
  --settings ASPNETCORE_ENVIRONMENT=Production Persistence__Provider=SqlServer BackendApi__BaseUrl="" WEBSITE_RUN_FROM_PACKAGE=1

az webapp config connection-string set `
  --resource-group $rg `
  --name $app `
  --connection-string-type SQLAzure `
  --settings TasteBudz="$conn"

az webapp config set `
  --resource-group $rg `
  --name $app `
  --web-sockets-enabled true `
  --always-on true `
  --min-tls-version 1.2
```

## Apply Database Scripts

Production schema deployment is manual. Do not replace this with startup migrations.

```powershell
$env:SQLCMDPASSWORD = $sqlPassword
$sqlHost = "tcp:$sqlServer.database.windows.net,1433"

sqlcmd -S $sqlHost -d $sqlDb -U $sqlAdmin -b -N -i ".\src\TasteBudz.Database\sqlserver\000_schema_versions.sql"
sqlcmd -S $sqlHost -d $sqlDb -U $sqlAdmin -b -N -i ".\src\TasteBudz.Database\sqlserver\010_schema.sql"
sqlcmd -S $sqlHost -d $sqlDb -U $sqlAdmin -b -N -i ".\src\TasteBudz.Database\sqlserver\020_seed_reference_data.sql"

Remove-Item Env:\SQLCMDPASSWORD
```

## Build, Publish, Zip

```powershell
dotnet restore TasteBudz.sln
dotnet build TasteBudz.sln -c Release
dotnet test TasteBudz.sln -c Release --no-build

$publishDir = Join-Path $PWD "artifacts\publish\TasteBudz.Web.Mvc"
$zipPath = Join-Path $PWD "artifacts\publish\TasteBudz.Web.Mvc.zip"

Remove-Item $publishDir, $zipPath -Recurse -Force -ErrorAction SilentlyContinue
dotnet publish .\src\TasteBudz.Web.Mvc\TasteBudz.Web.Mvc.csproj -c Release -o $publishDir
Compress-Archive -Path "$publishDir\*" -DestinationPath $zipPath -Force
```

## Deploy

```powershell
az webapp deploy `
  --resource-group $rg `
  --name $app `
  --src-path $zipPath `
  --type zip `
  --restart true
```

## Verify

```powershell
Start-Process "https://$app.azurewebsites.net"

curl.exe -k -i "https://$app.azurewebsites.net/api/v1/restaurants"
curl.exe -k -i -X POST "https://$app.azurewebsites.net/hubs/chat/negotiate?negotiateVersion=1"

az webapp log tail --resource-group $rg --name $app
```

Expected unauthenticated API and SignalR responses are `401`, not `404`. If the app fails on startup, check the log stream for SQL Server schema validation errors and confirm all scripts were applied.
