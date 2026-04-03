param environmentName string
param location string
param backendImage string
param frontendImage string

@secure()
@description('Full JSON content of appsettings.{environmentName}.json. When provided, mounted read-only at /run/secrets/appsettings.{environmentName}.json inside the backend container.')
param appSettingsJson string = ''

param tags object

// Log Analytics Workspace (shared by both Container Apps)
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

// Backend Container App
resource backendApp 'Microsoft.App/containerApps@2023-05-01' = {
  name: 'ca-ai-web-backend'
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
        targetPort: 8080
        transport: 'http'
      }
      secrets: !empty(appSettingsJson) ? [
        {
          name: 'appsettings-env-json'
          value: appSettingsJson
        }
      ] : []
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
          env: [
            {
              // Mirrors the azd environment name so Program.cs loads
              // /run/secrets/appsettings.{environmentName}.json
              name: 'ASPNETCORE_ENVIRONMENT'
              value: environmentName
            }
          ]
          volumeMounts: !empty(appSettingsJson) ? [
            {
              volumeName: 'appsettings-vol'
              mountPath: '/run/secrets'
            }
          ] : []
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

// Frontend Container App
resource frontendApp 'Microsoft.App/containerApps@2023-05-01' = {
  name: 'ca-ai-web-frontend'
  location: location
  tags: union(tags, { 'azd-service-name': 'frontend' })
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
    }
    template: {
      containers: [
        {
          name: 'frontend'
          image: frontendImage
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
          env: [
            {
              name: 'BACKEND_URL'
              value: 'https://${backendApp.properties.configuration.ingress.fqdn}'
            }
          ]
        }
      ]
      scale: {
        minReplicas: 0
        maxReplicas: 1
      }
    }
  }
}

output BACKEND_URI string = 'https://${backendApp.properties.configuration.ingress.fqdn}'
output FRONTEND_URI string = 'https://${frontendApp.properties.configuration.ingress.fqdn}'
output AZURE_CONTAINER_APPS_ENVIRONMENT_NAME string = cae.name
