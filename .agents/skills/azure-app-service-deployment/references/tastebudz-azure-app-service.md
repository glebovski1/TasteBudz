# TasteBudz Azure App Service Deployment Commands

Use these commands from the repository root. Replace placeholder values before running.

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
