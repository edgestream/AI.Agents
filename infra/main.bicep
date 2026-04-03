targetScope = 'subscription'

@minLength(1)
@maxLength(64)
@description('Name of the environment (e.g. dev, prod). Used to name all resources.')
param environmentName string

@minLength(1)
@description('Azure region for all resources.')
param location string

@description('Container image for the backend service (e.g. ghcr.io/org/ai-web-aguiserver:sha-abc1234).')
param backendImage string = 'ghcr.io/edgestream/ai-web-aguiserver:latest'

@description('Container image for the frontend service (e.g. ghcr.io/org/ai-web-aguichat:sha-abc1234).')
param frontendImage string = 'ghcr.io/edgestream/ai-web-aguichat:latest'

@description('Azure OpenAI endpoint URL.')
param azureOpenAIEndpoint string = ''

@description('Azure OpenAI deployment name.')
param azureOpenAIDeploymentName string = ''

@secure()
@description('Azure OpenAI API key.')
param azureOpenAIApiKey string = ''

@secure()
@description('Full JSON content of appsettings.{environmentName}.json. Loaded automatically from the repo root by the preprovision hook when the file exists.')
param appSettingsJson string = ''

var tags = {
  'azd-env-name': environmentName
}

// Resource Group
resource rg 'Microsoft.Resources/resourceGroups@2022-09-01' = {
  name: 'rg-ai-web-${toLower(environmentName)}'
  location: location
  tags: tags
}

module resources './resources.bicep' = {
  name: 'resources'
  scope: rg
  params: {
    environmentName: environmentName
    location: location
    backendImage: backendImage
    frontendImage: frontendImage
    azureOpenAIEndpoint: azureOpenAIEndpoint
    azureOpenAIDeploymentName: azureOpenAIDeploymentName
    azureOpenAIApiKey: azureOpenAIApiKey
    appSettingsJson: appSettingsJson
    tags: tags
  }
}

output BACKEND_URI string = resources.outputs.BACKEND_URI
output FRONTEND_URI string = resources.outputs.FRONTEND_URI
output AZURE_CONTAINER_APPS_ENVIRONMENT_NAME string = resources.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_NAME
