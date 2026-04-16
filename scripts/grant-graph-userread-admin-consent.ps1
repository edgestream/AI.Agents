#Requires -Version 7
<#
.SYNOPSIS
    Grants Microsoft Graph delegated User.Read permission and admin consent
    for the Entra app registration configured in an azd environment.

.PARAMETER EnvironmentName
    The azd environment folder name under .azure.
    Defaults to Production.

.EXAMPLE
    ./scripts/grant-graph-userread-admin-consent.ps1

.EXAMPLE
    ./scripts/grant-graph-userread-admin-consent.ps1 -EnvironmentName Production
#>
param(
    [string] $EnvironmentName = 'Production'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-AzdEnvValue {
    param(
        [string] $FilePath,
        [string] $Key
    )

    $line = Get-Content -Path $FilePath |
        Where-Object { $_ -match "^$([regex]::Escape($Key))=" } |
        Select-Object -First 1

    if (-not $line) {
        Write-Error "Key '$Key' not found in '$FilePath'."
    }

    $value = $line.Substring($Key.Length + 1).Trim()
    if ($value.Length -ge 2 -and $value.StartsWith('"') -and $value.EndsWith('"')) {
        $value = $value.Substring(1, $value.Length - 2)
    }

    return $value
}

if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
    Write-Error "Azure CLI not found. Install it from https://aka.ms/installazurecli"
}

$envFile = Join-Path $PSScriptRoot "..\.azure\$EnvironmentName\.env"
if (-not (Test-Path $envFile)) {
    Write-Error "Environment file '$envFile' was not found."
}

$tenantId = Get-AzdEnvValue -FilePath $envFile -Key 'ENTRA_TENANT_ID'
$clientId = Get-AzdEnvValue -FilePath $envFile -Key 'ENTRA_CLIENT_ID'
$subscriptionId = Get-AzdEnvValue -FilePath $envFile -Key 'AZURE_SUBSCRIPTION_ID'

$graphApiId = '00000003-0000-0000-c000-000000000000'
$graphUserReadScopeId = 'e1fe6dd8-ba31-4d61-89e7-88639da4683d'

$accountJson = az account show --output json 2>$null
if (-not $accountJson) {
    Write-Host "Not logged in. Running 'az login --tenant $tenantId'..."
    az login --tenant $tenantId | Out-Null
    $accountJson = az account show --output json
}

$account = $accountJson | ConvertFrom-Json
if ($account.tenantId -ne $tenantId) {
    Write-Host "Switching Azure CLI login to tenant '$tenantId'..."
    az login --tenant $tenantId | Out-Null
}

if ($subscriptionId) {
    az account set --subscription $subscriptionId
}

$app = az ad app show --id $clientId --query "{displayName:displayName, appId:appId}" --output json | ConvertFrom-Json
Write-Host "Target app registration: $($app.displayName) ($($app.appId))"

$hasUserRead = az ad app show --id $clientId --query "contains(join(',', requiredResourceAccess[?resourceAppId=='$graphApiId'].resourceAccess[].id), '$graphUserReadScopeId')" --output tsv
if ($hasUserRead -ne 'true') {
    Write-Host "Adding Microsoft Graph delegated permission 'User.Read'..."
    az ad app permission add --id $clientId --api $graphApiId --api-permissions "$graphUserReadScopeId=Scope" | Out-Null
}
else {
    Write-Host "Microsoft Graph delegated permission 'User.Read' is already configured."
}

Write-Host "Granting admin consent..."
az ad app permission admin-consent --id $clientId | Out-Null

Write-Host "Verifying effective grants..."
az ad app permission list --id $clientId --output json
az ad app permission list-grants --id $clientId --output json