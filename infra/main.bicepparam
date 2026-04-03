using './main.bicep'

param environmentName = readEnvironmentVariable('AZURE_ENV_NAME', 'Development')
param location = readEnvironmentVariable('AZURE_LOCATION', 'eastus2')

// Override image tags for CD: azd env set BACKEND_IMAGE ghcr.io/edgestream/ai-web-aguiserver:sha-<7-char-SHA>
param backendImage = readEnvironmentVariable('BACKEND_IMAGE', 'ghcr.io/edgestream/ai-web-aguiserver:latest')
param frontendImage = readEnvironmentVariable('FRONTEND_IMAGE', 'ghcr.io/edgestream/ai-web-aguichat:latest')

// Azure OpenAI configuration – set via: azd env set AZURE_OPENAI_ENDPOINT <value>
param azureOpenAIEndpoint = readEnvironmentVariable('AZURE_OPENAI_ENDPOINT', '')
param azureOpenAIDeploymentName = readEnvironmentVariable('AZURE_OPENAI_DEPLOYMENT_NAME', '')
param azureOpenAIApiKey = readEnvironmentVariable('AZURE_OPENAI_API_KEY', '')

// Appsettings for this environment – auto-loaded from appsettings.{AZURE_ENV_NAME}.json by the preprovision hook.
// e.g. 'azd env new Development' reads appsettings.Development.json; 'azd env new Production' reads appsettings.Production.json.
param appSettingsJson = readEnvironmentVariable('APPSETTINGS_JSON', '')
