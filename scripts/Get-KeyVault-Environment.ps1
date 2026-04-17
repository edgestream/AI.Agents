#Requires -Version 7
<#
.SYNOPSIS
    Pulls local Entra app credentials from Azure Key Vault into a local .env file.

.PARAMETER VaultName
    Name of the Azure Key Vault holding the shared dev credentials.
    Defaults to the AGENTS_KEYVAULT env var if set.

.EXAMPLE
    ./scripts/Get-KeyVault-Environment.ps1 -VaultName kv-edgestream-dev
#>
param(
    [string] $VaultName = $env:AGENTS_KEYVAULT
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if (-not $VaultName) {
    Write-Error "Provide -VaultName or set AGENTS_KEYVAULT."
}

if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
    Write-Error "Azure CLI not found. Install it from https://aka.ms/installazurecli"
}

$account = az account show 2>$null | ConvertFrom-Json
if (-not $account) {
    Write-Host "Not logged in. Running 'az login'..."
    az login | Out-Null
}

Write-Host "Fetching credentials from Key Vault '$VaultName'..."

$credentials = @(
    @{ KeySuffix = 'TENANT_ID'; SecretName = 'AGENTS-ENTRA-TENANT-ID' }
    @{ KeySuffix = 'CLIENT_ID'; SecretName = 'AGENTS-ENTRA-CLIENT-ID' }
    @{ KeySuffix = 'CLIENT_SECRET'; SecretName = 'AGENTS-ENTRA-CLIENT-SECRET' }
)

function Get-SecretValue {
    param(
        [string] $VaultName,
        [string] $SecretName
    )

    $value = az keyvault secret show --vault-name $VaultName --name $SecretName --query value -o tsv 2>$null
    if ($LASTEXITCODE -eq 0 -and -not [string]::IsNullOrWhiteSpace($value)) {
        return $value
    }

    Write-Error "Failed to fetch secret '$SecretName' from vault '$VaultName'."
}

$lines = foreach ($credential in $credentials) {
    $value = Get-SecretValue -VaultName $VaultName -SecretName $credential.SecretName
    @(
        "ENTRA_$($credential.KeySuffix)=$value"
        "AZURE_$($credential.KeySuffix)=$value"
    )
}

$envFile = Join-Path $PSScriptRoot '..' '.env'
$lines | Set-Content -Path $envFile -Encoding utf8

Write-Host ".env written to $((Resolve-Path $envFile).Path)"