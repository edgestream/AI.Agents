#Requires -Version 7
<#
.SYNOPSIS
    Pulls AI.Agents.Server service principal credentials from Azure Key Vault into a local .env file.

.PARAMETER VaultName
    Name of the Azure Key Vault holding the shared dev credentials.
    Defaults to the AGUI_KEY_VAULT env var if set.

.EXAMPLE
    ./scripts/init-env.ps1 -VaultName kv-edgestream-dev
#>
param(
    [string] $VaultName = $env:AGUI_KEY_VAULT
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if (-not $VaultName) {
    Write-Error "Provide -VaultName or set the AGUI_KEY_VAULT environment variable."
}

# Verify Azure CLI is available and logged in
if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
    Write-Error "Azure CLI not found. Install it from https://aka.ms/installazurecli"
}

$account = az account show 2>$null | ConvertFrom-Json
if (-not $account) {
    Write-Host "Not logged in. Running 'az login'..."
    az login | Out-Null
}

Write-Host "Fetching credentials from Key Vault '$VaultName'..."

$secrets = @(
    @{ EnvKey = 'AZURE_TENANT_ID';     VaultKey = 'AGUI-AZURE-TENANT-ID'     }
    @{ EnvKey = 'AZURE_CLIENT_ID';     VaultKey = 'AGUI-AZURE-CLIENT-ID'     }
    @{ EnvKey = 'AZURE_CLIENT_SECRET'; VaultKey = 'AGUI-AZURE-CLIENT-SECRET' }
)

$lines = foreach ($s in $secrets) {
    $value = az keyvault secret show --vault-name $VaultName --name $s.VaultKey --query value -o tsv
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to fetch secret '$($s.VaultKey)' from vault '$VaultName'."
    }
    "$($s.EnvKey)=$value"
}

$envFile = Join-Path $PSScriptRoot '..' '.env'
$lines | Set-Content -Path $envFile -Encoding utf8

Write-Host ".env written to $((Resolve-Path $envFile).Path)"
