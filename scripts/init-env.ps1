#Requires -Version 7
<#
.SYNOPSIS
    Pulls AI.Agents.Server service principal credentials from Azure Key Vault into a local .env file.

.PARAMETER VaultName
    Name of the Azure Key Vault holding the shared dev credentials.
    Defaults to the AI_AGENTS_KEY_VAULT env var if set.
    Falls back to AGUI_KEY_VAULT during migration.

.EXAMPLE
    ./scripts/init-env.ps1 -VaultName kv-edgestream-dev
#>
param(
    [string] $VaultName = $env:AI_AGENTS_KEY_VAULT
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if (-not $VaultName) {
    $VaultName = $env:AGUI_KEY_VAULT
}

if (-not $VaultName) {
    Write-Error "Provide -VaultName or set AI_AGENTS_KEY_VAULT (legacy: AGUI_KEY_VAULT)."
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
    @{ EnvKey = 'AZURE_TENANT_ID';     VaultKeys = @('AI-AGENTS-AZURE-TENANT-ID', 'AGUI-AZURE-TENANT-ID')     }
    @{ EnvKey = 'AZURE_CLIENT_ID';     VaultKeys = @('AI-AGENTS-AZURE-CLIENT-ID', 'AGUI-AZURE-CLIENT-ID')     }
    @{ EnvKey = 'AZURE_CLIENT_SECRET'; VaultKeys = @('AI-AGENTS-AZURE-CLIENT-SECRET', 'AGUI-AZURE-CLIENT-SECRET') }
)

function Get-SecretValue {
    param(
        [string] $VaultName,
        [string[]] $SecretNames
    )

    foreach ($secretName in $SecretNames) {
        $value = az keyvault secret show --vault-name $VaultName --name $secretName --query value -o tsv 2>$null
        if ($LASTEXITCODE -eq 0 -and -not [string]::IsNullOrWhiteSpace($value)) {
            return $value
        }
    }

    Write-Error "Failed to fetch any of these secrets from vault '$VaultName': $($SecretNames -join ', ')"
}

$lines = foreach ($s in $secrets) {
    $value = Get-SecretValue -VaultName $VaultName -SecretNames $s.VaultKeys
    "$($s.EnvKey)=$value"
}

$envFile = Join-Path $PSScriptRoot '..' '.env'
$lines | Set-Content -Path $envFile -Encoding utf8

Write-Host ".env written to $((Resolve-Path $envFile).Path)"
