param environmentName string
param location string
param backendImage string
param frontendImage string

@description('Azure OpenAI endpoint URL. When set, overrides the value from the mounted appsettings file.')
param azureOpenAIEndpoint string = ''

@description('Azure OpenAI deployment name. When set, overrides the value from the mounted appsettings file.')
param azureOpenAIDeploymentName string = ''

@secure()
@description('Azure OpenAI API key. When set, overrides the value from the mounted appsettings file.')
param azureOpenAIApiKey string = ''

@secure()
@description('Full JSON content of appsettings.{environmentName}.json. When provided, mounted read-only at /run/secrets/appsettings.{environmentName}.json inside the backend container.')
param appSettingsJson string = ''

@description('Microsoft Entra app registration client ID. When set, enables Easy Auth on the Container App ingress.')
param entraClientId string = ''

@secure()
@description('Microsoft Entra app registration client secret. Required when entraClientId is set.')
param entraClientSecret string = ''

@description('Microsoft Entra tenant ID. Required when entraClientId is set.')
param entraTenantId string = ''

param tags object

// Log Analytics Workspace
resource logWorkspace 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: 'log-ai-web-${toLower(environmentName)}'
  location: location
  tags: tags
  properties: {
    retentionInDays: 30
    sku: {
      name: 'PerGB2018'
    }
  }
}

// Container Apps Environment (Consumption tier)
resource cae 'Microsoft.App/managedEnvironments@2023-05-01' = {
  name: 'cae-ai-web-${toLower(environmentName)}'
  location: location
  tags: tags
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logWorkspace.properties.customerId
        sharedKey: logWorkspace.listKeys().primarySharedKey
      }
    }
  }
}

// Combined Container App: frontend (ingress on 3000) + backend sidecar (localhost:8080).
// Mirrors the docker-compose single-host layout. Halves ACA billing for personal deployments
// where independent scaling of the two containers is not required.
resource app 'Microsoft.App/containerApps@2023-05-01' = {
  name: 'ca-ai-web'
  location: location
  tags: union(tags, { 'azd-service-name': 'backend' })
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    managedEnvironmentId: cae.id
    configuration: {
      ingress: {
        external: true
        targetPort: 3000
        transport: 'http'
      }
      secrets: concat(
        !empty(azureOpenAIApiKey) ? [
          {
            name: 'azure-openai-api-key'
            value: azureOpenAIApiKey
          }
        ] : [],
        !empty(appSettingsJson) ? [
          {
            name: 'appsettings-env-json'
            value: appSettingsJson
          }
        ] : [],
        !empty(entraClientSecret) ? [
          {
            name: 'entra-client-secret'
            value: entraClientSecret
          }
        ] : []
      )
    }
    template: {
      containers: [
        {
          name: 'backend'
          image: backendImage
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
          env: concat([
            {
              // Mirrors the azd environment name so Program.cs loads
              // /run/secrets/appsettings.{environmentName}.json
              name: 'ASPNETCORE_ENVIRONMENT'
              value: environmentName
            }
          ], !empty(azureOpenAIEndpoint) ? [
            {
              // CD override: azd env set AZURE_OPENAI_ENDPOINT <value>
              name: 'AzureOpenAI__Endpoint'
              value: azureOpenAIEndpoint
            }
          ] : [], !empty(azureOpenAIDeploymentName) ? [
            {
              // CD override: azd env set AZURE_OPENAI_DEPLOYMENT_NAME <value>
              name: 'AzureOpenAI__DeploymentName'
              value: azureOpenAIDeploymentName
            }
          ] : [], !empty(azureOpenAIApiKey) ? [
            {
              // CD override: azd env set AZURE_OPENAI_API_KEY <value>
              name: 'AzureOpenAI__ApiKey'
              secretRef: 'azure-openai-api-key'
            }
          ] : [])
          volumeMounts: !empty(appSettingsJson) ? [
            {
              volumeName: 'appsettings-vol'
              mountPath: '/run/secrets'
            }
          ] : []
        }
        {
          name: 'frontend'
          image: frontendImage
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
          env: [
            {
              // Backend is co-located in the same replica; communicate over localhost.
              name: 'BACKEND_URL'
              value: 'http://localhost:8080'
            }
          ]
        }
      ]
      volumes: !empty(appSettingsJson) ? [
        {
          name: 'appsettings-vol'
          storageType: 'Secret'
          secrets: [
            {
              secretRef: 'appsettings-env-json'
              path: 'appsettings.${environmentName}.json'
            }
          ]
        }
      ] : []
      scale: {
        minReplicas: 0
        maxReplicas: 1
      }
    }
  }
}

resource appAuthConfig 'Microsoft.App/containerApps/authConfigs@2024-03-01' = if (!empty(entraClientId)) {
  parent: app
  name: 'current'
  properties: {
    platform: {
      enabled: true
    }
    globalValidation: {
      unauthenticatedClientAction: 'RedirectToLoginPage'
    }
    identityProviders: {
      azureActiveDirectory: {
        registration: {
          clientId: entraClientId
          clientSecretSettingName: 'entra-client-secret'
          openIdIssuer: '${environment().authentication.loginEndpoint}${entraTenantId}/v2.0'
        }
        validation: {
          allowedAudiences: [
            entraClientId
          ]
        }
      }
    }
  }
}

output APP_URI string = 'https://${app.properties.configuration.ingress.fqdn}'
output AZURE_CONTAINER_APPS_ENVIRONMENT_NAME string = cae.name
