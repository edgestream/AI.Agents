using './main.bicep'

param environmentName = readEnvironmentVariable('AZURE_ENV_NAME', 'dev')
param location = readEnvironmentVariable('AZURE_LOCATION', 'eastus')

// Override image tags for CD: azd env set BACKEND_IMAGE ghcr.io/edgestream/ai-web-aguiserver:sha-<7-char-SHA>
param backendImage = readEnvironmentVariable('BACKEND_IMAGE', 'ghcr.io/edgestream/ai-web-aguiserver:latest')
param frontendImage = readEnvironmentVariable('FRONTEND_IMAGE', 'ghcr.io/edgestream/ai-web-aguichat:latest')

// Azure OpenAI configuration – set via: azd env set AZURE_OPENAI_ENDPOINT <value>
param azureOpenAIEndpoint = readEnvironmentVariable('AZURE_OPENAI_ENDPOINT', '')
param azureOpenAIDeploymentName = readEnvironmentVariable('AZURE_OPENAI_DEPLOYMENT_NAME', '')
param azureOpenAIApiKey = readEnvironmentVariable('AZURE_OPENAI_API_KEY', '')

// Production appsettings (McpServers config) – set via: azd env set APPSETTINGS_JSON '{"McpServers": {...}}'
param appSettingsJson = readEnvironmentVariable('APPSETTINGS_JSON', '')
