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
  name: 'log-ai-agui-${toLower(environmentName)}'
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
  name: 'cae-ai-agui-${toLower(environmentName)}'
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
  name: 'ca-ai-agui'
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
            {
              // Next.js standalone server binds to the HOSTNAME env var. In Container Apps the
              // runtime sets HOSTNAME to the replica name, so Next.js would only listen on that
              // DNS name — not on 127.0.0.1. Easy Auth forwards to 127.0.0.1:3000, so we must
              // override HOSTNAME to 0.0.0.0 to make Next.js listen on all interfaces.
              name: 'HOSTNAME'
              value: '0.0.0.0'
            }
          ]
          // Readiness probe ensures the pod is not marked ready until Next.js is listening.
          // Without this, Easy Auth receives traffic before port 3000 is open on cold starts,
          // causing 500 "Connection refused" immediately after the Entra login callback.
          probes: [
            {
              type: 'readiness'
              httpGet: {
                path: '/'
                port: 3000
                scheme: 'HTTP'
              }
              initialDelaySeconds: 2
              periodSeconds: 3
              failureThreshold: 10
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
        // Keep 1 replica warm when Easy Auth is enabled: Easy Auth's http-auth sidecar starts
        // The readiness probe on the frontend container gates the Container Apps ingress:
        // traffic is only routed to a replica once Next.js passes its probe on port 3000.
        // HOSTNAME=0.0.0.0 ensures Next.js binds to all interfaces so Easy Auth's proxy
        // to 127.0.0.1:3000 succeeds. Scale-to-zero is therefore safe with Easy Auth.
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
        login: {
          // Request Graph User.Read scope so the access token can be used
          // to retrieve the user's display name and profile photo from Microsoft Graph.
          // See: https://learn.microsoft.com/azure/app-service/scenario-secure-app-access-microsoft-graph-as-user
          loginParameters: [
            'response_type=code id_token'
            'scope=openid offline_access profile https://graph.microsoft.com/User.Read'
          ]
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
