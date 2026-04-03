using './main.bicep'

param environmentName = readEnvironmentVariable('AZURE_ENV_NAME', 'Development')
param location = readEnvironmentVariable('AZURE_LOCATION', 'eastus2')

// Override image tags for CD: azd env set BACKEND_IMAGE ghcr.io/edgestream/ai-web-aguiserver:sha-<7-char-SHA>
param backendImage = readEnvironmentVariable('BACKEND_IMAGE', 'ghcr.io/edgestream/ai-web-aguiserver:latest')
param frontendImage = readEnvironmentVariable('FRONTEND_IMAGE', 'ghcr.io/edgestream/ai-web-aguichat:latest')

// Azure OpenAI configuration – CD override via: azd env set AZURE_OPENAI_ENDPOINT <value>
// When set, these values override the corresponding keys from the mounted appsettings file.
param azureOpenAIEndpoint = readEnvironmentVariable('AZURE_OPENAI_ENDPOINT', '')
param azureOpenAIDeploymentName = readEnvironmentVariable('AZURE_OPENAI_DEPLOYMENT_NAME', '')
param azureOpenAIApiKey = readEnvironmentVariable('AZURE_OPENAI_API_KEY', '')

// Appsettings for this environment – auto-loaded from appsettings.{AZURE_ENV_NAME}.json by the preprovision hook.
// e.g. 'azd env new Development' reads appsettings.Development.json; 'azd env new Production' reads appsettings.Production.json.
param appSettingsJson = readEnvironmentVariable('APPSETTINGS_JSON', '')

// Microsoft Entra authentication – set these to enable Easy Auth on the Container App ingress.
// azd env set ENTRA_CLIENT_ID <app-registration-client-id>
// azd env set ENTRA_CLIENT_SECRET <client-secret-value>
// azd env set ENTRA_TENANT_ID <tenant-id>
param entraClientId = readEnvironmentVariable('ENTRA_CLIENT_ID', '')
param entraClientSecret = readEnvironmentVariable('ENTRA_CLIENT_SECRET', '')
param entraTenantId = readEnvironmentVariable('ENTRA_TENANT_ID', '')
