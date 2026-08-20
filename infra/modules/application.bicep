extension microsoftGraphV1

param environmentName string
param location string
param tags object
param containerImage string
@secure()
param entraTenantId string
param foundryModelDeploymentName string = 'gpt-4o'
param foundryModelIdentity string = 'gpt-4o'
param foundryModelName string = 'gpt-4o'
param foundryModelVersion string = '2024-11-20'
param guardrailMode string
param guardrailApprovedBy string
param guardrailRollbackReference string
param fabricGovernance object

var suffix = uniqueString(resourceGroup().id)
var identityName = 'id-oe-${environmentName}'
var logAnalyticsName = 'log-oe-${environmentName}'
var appInsightsName = 'appi-oe-${environmentName}'
var containerEnvironmentName = 'cae-oe-${environmentName}'
var containerAppName = 'ca-oe-${environmentName}'
var cosmosName = 'cosmos-oe-${suffix}'
var serviceBusName = 'sb-oe-${suffix}'
var containerRegistryName = 'cr${uniqueString(resourceGroup().id, environmentName)}'
var foundryAccountName = 'oai-oe-${environmentName}'
var foundryProjectName = 'proj-${environmentName}'
var foundryProjectEndpoint = 'https://${foundryAccountName}.services.ai.azure.com/api/projects/${foundryProjectName}'
var databaseName = 'opportunity-engineering'
var containerName = 'workspace-data'
var eventTopicName = 'graph-events'
var reviewSubscriptionName = 'review-projection'
var recommendationQueueName = 'recommendations'
var signalRName = 'sigr-oe-${environmentName}'
// ponytail: deterministic per-resource-group value, not rotatable — stored in Key Vault below
// (see participantSigningKeySecret) rather than passed to the container as a plaintext env
// value. Add rotation only if participant-token compromise ever matters beyond a single ~4h
// join-code session.
var participantSigningKey = '${guid(resourceGroup().id, 'participant-signing-key-a')}${guid(resourceGroup().id, 'participant-signing-key-b')}'

resource identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2024-11-30' = {
  name: identityName
  location: location
  tags: tags
}

// --- Entra ID resources via Microsoft Graph ---

resource facilitatorGroup 'Microsoft.Graph/groups@v1.0' = {
  displayName: 'OE Facilitators'
  mailEnabled: false
  securityEnabled: true
  mailNickname: 'oe-facilitators'
  uniqueName: 'oe-facilitators-${environmentName}'
  description: 'Opportunity Engineering Facilitator role group'
}

resource reviewerGroup 'Microsoft.Graph/groups@v1.0' = {
  displayName: 'OE Reviewers'
  mailEnabled: false
  securityEnabled: true
  mailNickname: 'oe-reviewers'
  uniqueName: 'oe-reviewers-${environmentName}'
  description: 'Opportunity Engineering Reviewer role group'
}

resource apiApp 'Microsoft.Graph/applications@v1.0' = {
  displayName: 'opportunity-engineering-api'
  uniqueName: 'opportunity-engineering-${environmentName}'
  signInAudience: 'AzureADMyOrg'
  identifierUris: [
    'api://opportunity-engineering-${environmentName}'
  ]
  api: {
    oauth2PermissionScopes: [
      {
        id: 'eabc9964-0f07-5d38-abc1-f5523937bb4b'
        adminConsentDescription: 'Allows the frontend to call the Opportunity Engineering API on behalf of the signed-in user.'
        adminConsentDisplayName: 'Access Opportunity Engineering as the signed-in user'
        userConsentDescription: 'Allows the app to call the Opportunity Engineering API on your behalf.'
        userConsentDisplayName: 'Access Opportunity Engineering'
        value: 'access_as_user'
        type: 'User'
        isEnabled: true
      }
    ]
  }
  web: {
    redirectUris: []
  }
  requiredResourceAccess: []
}

resource apiServicePrincipal 'Microsoft.Graph/servicePrincipals@v1.0' = {
  appId: apiApp.appId
}

// Shape must match WorkspaceAuthorizationSettings/WorkspaceRoleMapping in
// src/OpportunityEngineering.Api/Authorization/WorkspaceAuthorization.cs exactly: a
// dictionary keyed by workspace ID, each value holding plural *GroupObjectIds arrays.
var workspaceAuthorization = {
  workspaces: {
    'workspace-${environmentName}': {
      facilitatorGroupObjectIds: [
        facilitatorGroup.id
      ]
      reviewerGroupObjectIds: [
        reviewerGroup.id
      ]
    }
  }
}

// --- Azure resources ---

resource logs 'Microsoft.OperationalInsights/workspaces@2025-07-01' = {
  name: logAnalyticsName
  location: location
  tags: tags
  properties: {
    retentionInDays: 90
    features: {
      enableLogAccessUsingOnlyResourcePermissions: true
    }
  }
}

resource insights 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsightsName
  location: location
  kind: 'web'
  tags: tags
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logs.id
    IngestionMode: 'LogAnalytics'
    RetentionInDays: 90
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
  }
}

resource containerEnvironment 'Microsoft.App/managedEnvironments@2025-07-01' = {
  name: containerEnvironmentName
  location: location
  tags: tags
  properties: {
    appLogsConfiguration: {
      destination: 'azure-monitor'
    }
    zoneRedundant: false
    workloadProfiles: [
      {
        name: 'Consumption'
        workloadProfileType: 'Consumption'
      }
    ]
  }
}

resource containerEnvironmentDiagnostics 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  name: 'diag-${containerEnvironmentName}'
  scope: containerEnvironment
  properties: {
    workspaceId: logs.id
    logs: [
      {
        category: 'ContainerAppConsoleLogs'
        enabled: true
      }
      {
        category: 'ContainerAppSystemLogs'
        enabled: true
      }
    ]
  }
}

resource cosmos 'Microsoft.DocumentDB/databaseAccounts@2025-04-15' = {
  name: cosmosName
  location: location
  tags: tags
  kind: 'GlobalDocumentDB'
  properties: {
    databaseAccountOfferType: 'Standard'
    disableKeyBasedMetadataWriteAccess: true
    disableLocalAuth: true
    enableAutomaticFailover: false
    enableMultipleWriteLocations: false
    locations: [
      {
        locationName: location
        failoverPriority: 0
        isZoneRedundant: true
      }
    ]
    consistencyPolicy: {
      defaultConsistencyLevel: 'Session'
    }
    backupPolicy: {
      type: 'Continuous'
      continuousModeProperties: {
        tier: 'Continuous30Days'
      }
    }
    minimalTlsVersion: 'Tls12'
    publicNetworkAccess: 'Enabled'
  }
}

resource database 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2025-04-15' = {
  parent: cosmos
  name: databaseName
  properties: {
    resource: {
      id: databaseName
    }
  }
}

resource dataContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2025-04-15' = {
  parent: database
  name: containerName
  properties: {
    resource: {
      id: containerName
      partitionKey: {
        paths: [
          '/workspaceId'
        ]
        kind: 'Hash'
        version: 2
      }
      defaultTtl: -1
      indexingPolicy: {
        indexingMode: 'consistent'
        automatic: true
        includedPaths: [
          {
            path: '/*'
          }
        ]
        excludedPaths: [
          {
            path: '/"_etag"/?'
          }
        ]
        compositeIndexes: [
          [
            {
              path: '/documentType'
              order: 'ascending'
            }
            {
              path: '/payload/occurredAt'
              order: 'ascending'
            }
          ]
          [
            {
              path: '/documentType'
              order: 'ascending'
            }
            {
              path: '/payload/createdAt'
              order: 'ascending'
            }
          ]
        ]
      }
    }
    options: {
      autoscaleSettings: {
        maxThroughput: 1000
      }
    }
  }
}

resource serviceBus 'Microsoft.ServiceBus/namespaces@2024-01-01' = {
  name: serviceBusName
  location: location
  tags: tags
  sku: {
    name: 'Standard'
    tier: 'Standard'
  }
  properties: {
    disableLocalAuth: true
    minimumTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
    zoneRedundant: false
  }
}

resource eventTopic 'Microsoft.ServiceBus/namespaces/topics@2024-01-01' = {
  parent: serviceBus
  name: eventTopicName
  properties: {
    defaultMessageTimeToLive: 'P14D'
    duplicateDetectionHistoryTimeWindow: 'P1D'
    enableBatchedOperations: true
    enableExpress: false
    requiresDuplicateDetection: true
    supportOrdering: true
  }
}

resource reviewSubscription 'Microsoft.ServiceBus/namespaces/topics/subscriptions@2024-01-01' = {
  parent: eventTopic
  name: reviewSubscriptionName
  properties: {
    deadLetteringOnMessageExpiration: true
    defaultMessageTimeToLive: 'P14D'
    enableBatchedOperations: true
    lockDuration: 'PT2M'
    maxDeliveryCount: 5
    requiresSession: true
  }
}

resource recommendationQueue 'Microsoft.ServiceBus/namespaces/queues@2024-01-01' = {
  parent: serviceBus
  name: recommendationQueueName
  properties: {
    deadLetteringOnMessageExpiration: true
    defaultMessageTimeToLive: 'P14D'
    duplicateDetectionHistoryTimeWindow: 'P1D'
    enableBatchedOperations: true
    enableExpress: false
    lockDuration: 'PT2M'
    maxDeliveryCount: 10
    requiresDuplicateDetection: true
    requiresSession: true
  }
}

resource signalR 'Microsoft.SignalRService/signalR@2024-03-01' = {
  name: signalRName
  location: location
  tags: tags
  sku: {
    // ponytail: Free_F1 caps at 20 concurrent connections / 20,000 messages per day.
    // Upgrade to Standard_S1 if a workshop room approaches that limit — SKU-only change.
    name: 'Free_F1'
    capacity: 1
  }
  kind: 'SignalR'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    features: [
      {
        flag: 'ServiceMode'
        value: 'Default'
      }
    ]
    cors: {
      allowedOrigins: [
        '*'
      ]
    }
    disableLocalAuth: true
    publicNetworkAccess: 'Enabled'
  }
}

resource signalRAppServerRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(signalR.id, identity.id, 'signalr-app-server')
  scope: signalR
  properties: {
    principalId: identity.properties.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId(
      'Microsoft.Authorization/roleDefinitions',
      '420fcaa2-552c-430f-98ca-3264be4806c7'
    )
  }
}

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: 'kv-oe-${suffix}'
  location: location
  tags: tags
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: subscription().tenantId
    enableRbacAuthorization: true
    enableSoftDelete: true
  }
}

// Stable across redeploys (derived from the resource group, not freshly randomized) so a
// redeploy never invalidates a live join session mid-workshop — see the container app's
// Participant__SigningKey secretRef below, which is what actually keeps the value out of
// plaintext ARM/portal output for anyone with only Reader on the resource group. Rotation
// would need a deploymentScript-generated value instead; not worth the added complexity while
// participant-token compromise stays bounded to a single ~4h join-code session (the Participant
// scheme structurally cannot touch the canonical graph).
resource participantSigningKeySecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'participant-signing-key'
  properties: {
    value: participantSigningKey
  }
}

resource keyVaultSecretsUserRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVault.id, identity.id, 'key-vault-secrets-user')
  scope: keyVault
  properties: {
    principalId: identity.properties.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId(
      'Microsoft.Authorization/roleDefinitions',
      '4633458b-17de-408a-b874-0445c86b69e6'
    )
  }
}

#disable-next-line BCP081 // CognitiveServices type registry lag
resource foundryAccount 'Microsoft.CognitiveServices/accounts@2026-05-01' = {
  name: foundryAccountName
  location: location
  tags: tags
  kind: 'AIServices'
  sku: {
    name: 'S0'
  }
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    publicNetworkAccess: 'Enabled'
    disableLocalAuth: true
    allowProjectManagement: true
    customSubDomainName: foundryAccountName
  }
}

#disable-next-line BCP081 // CognitiveServices type registry lag
resource modelDeployment 'Microsoft.CognitiveServices/accounts/deployments@2026-05-01' = {
  parent: foundryAccount
  name: foundryModelDeploymentName
  sku: {
    name: 'GlobalStandard'
    capacity: 10
  }
  properties: {
    model: {
      format: 'OpenAI'
      name: foundryModelName
      version: empty(foundryModelVersion) ? null : foundryModelVersion
    }
  }
}

#disable-next-line BCP081 // CognitiveServices type registry lag
resource foundryProject 'Microsoft.CognitiveServices/accounts/projects@2026-05-01' = {
  parent: foundryAccount
  name: foundryProjectName
  location: location
  kind: 'AIServices'
  identity: {
    type: 'SystemAssigned'
  }
}

resource serviceBusSenderRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(serviceBus.id, identity.id, 'service-bus-sender')
  scope: serviceBus
  properties: {
    principalId: identity.properties.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId(
      'Microsoft.Authorization/roleDefinitions',
      '69a216fc-b8fb-44d8-bc22-1f3c2cd27a39'
    )
  }
}

resource serviceBusReceiverRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(serviceBus.id, identity.id, 'service-bus-receiver')
  scope: serviceBus
  properties: {
    principalId: identity.properties.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId(
      'Microsoft.Authorization/roleDefinitions',
      '4f6d3b9b-027b-4f4c-9142-0e5a2a2247e0'
    )
  }
}

resource foundryDeveloperRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(foundryProject.id, identity.id, 'foundry-developer')
  scope: foundryProject
  properties: {
    principalId: identity.properties.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId(
      'Microsoft.Authorization/roleDefinitions',
      '64702f94-c441-49e6-a78b-ef80e0188fee'
    )
  }
}

resource cosmosDataRole 'Microsoft.DocumentDB/databaseAccounts/sqlRoleAssignments@2025-04-15' = {
  parent: cosmos
  name: guid(cosmos.id, identity.id, 'cosmos-data-contributor')
  properties: {
    principalId: identity.properties.principalId
    roleDefinitionId: '${cosmos.id}/sqlRoleDefinitions/00000000-0000-0000-0000-000000000002'
    scope: cosmos.id
  }
}

resource containerRegistry 'Microsoft.ContainerRegistry/registries@2023-11-01-preview' = {
  name: containerRegistryName
  location: location
  tags: tags
  sku: {
    name: 'Basic'
  }
  properties: {
    adminUserEnabled: false
  }
}

resource acrPullRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(containerRegistry.id, identity.id, 'acr-pull')
  scope: containerRegistry
  properties: {
    principalId: identity.properties.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId(
      'Microsoft.Authorization/roleDefinitions',
      '7f951dda-4ed3-4680-a7ca-43fe172d538d'
    )
  }
}

resource app 'Microsoft.App/containerApps@2025-07-01' = {
  name: containerAppName
  location: location
  tags: union(tags, { 'azd-service-name': 'api' })
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${identity.id}': {}
    }
  }
  properties: {
    environmentId: containerEnvironment.id
    workloadProfileName: 'Consumption'
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        allowInsecure: false
        external: true
        targetPort: 8080
        transport: 'auto'
      }
      maxInactiveRevisions: 5
      registries: [
        {
          server: containerRegistry.properties.loginServer
          identity: identity.id
        }
      ]
      secrets: [
        {
          name: 'participant-signing-key'
          keyVaultUrl: participantSigningKeySecret.properties.secretUri
          identity: identity.id
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'api'
          image: containerImage
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
          env: [
            {
              name: 'AZURE_CLIENT_ID'
              value: identity.properties.clientId
            }
            {
              name: 'AzureAd__Instance'
              value: environment().authentication.loginEndpoint
            }
            {
              name: 'AzureAd__TenantId'
              value: entraTenantId
            }
            {
              name: 'AzureAd__ClientId'
              value: apiApp.appId
            }
            {
              name: 'Cors__AllowedOrigins__0'
              value: 'https://${staticWebApp.properties.defaultHostname}'
            }
            {
              name: 'Cosmos__AccountEndpoint'
              value: cosmos.properties.documentEndpoint
            }
            {
              name: 'Cosmos__DatabaseName'
              value: databaseName
            }
            {
              name: 'Cosmos__ContainerName'
              value: containerName
            }
            {
              name: 'ServiceBus__FullyQualifiedNamespace'
              value: '${serviceBus.name}.servicebus.windows.net'
            }
            {
              name: 'ServiceBus__GraphEventsTopic'
              value: eventTopic.name
            }
            {
              name: 'ServiceBus__RecommendationQueue'
              value: recommendationQueue.name
            }
            {
              name: 'ServiceBus__ReviewSubscription'
              value: reviewSubscription.name
            }
            {
              name: 'Foundry__ProjectEndpoint'
              value: foundryProjectEndpoint
            }
            {
              name: 'Foundry__ModelDeploymentName'
              value: foundryModelDeploymentName
            }
            {
              name: 'Foundry__ModelIdentity'
              value: foundryModelIdentity
            }
            {
              name: 'WorkspaceAuthorizationJson'
              value: string(workspaceAuthorization)
            }
            {
              name: 'FabricJson'
              value: string(fabricGovernance)
            }
            {
              name: 'Guardrails__Mode'
              value: guardrailMode
            }
            {
              name: 'Guardrails__ApprovedBy'
              value: guardrailApprovedBy
            }
            {
              name: 'Guardrails__RollbackReference'
              value: guardrailRollbackReference
            }
            {
              name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
              value: insights.properties.ConnectionString
            }
            {
              name: 'SignalR__Endpoint'
              value: 'https://${signalR.properties.hostName}'
            }
            {
              name: 'Participant__SigningKey'
              secretRef: 'participant-signing-key'
            }
          ]
          probes: [
            {
              type: 'Liveness'
              httpGet: {
                path: '/health/live'
                port: 8080
                scheme: 'HTTP'
              }
              initialDelaySeconds: 10
              periodSeconds: 30
              timeoutSeconds: 5
              failureThreshold: 3
            }
            {
              type: 'Readiness'
              httpGet: {
                path: '/health/ready'
                port: 8080
                scheme: 'HTTP'
              }
              initialDelaySeconds: 15
              periodSeconds: 30
              timeoutSeconds: 10
              failureThreshold: 3
            }
          ]
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 10
        rules: [
          {
            name: 'http-concurrency'
            http: {
              metadata: {
                concurrentRequests: '20'
              }
            }
          }
        ]
      }
    }
  }
  // Ensures every RBAC grant the container relies on at startup — including the Key Vault
  // Secrets User role for resolving the participant-signing-key secretRef — exists first.
  dependsOn: [
    cosmosDataRole
    serviceBusSenderRole
    serviceBusReceiverRole
    foundryDeveloperRole
    signalRAppServerRole
    keyVaultSecretsUserRole
  ]
}

// Azure Static Web Apps is not offered in every region; this adopts the already-provisioned
// resource (created out-of-band before this file tracked it) rather than relocating it.
resource staticWebApp 'Microsoft.Web/staticSites@2024-04-01' = {
  name: 'stapp-oe-${environmentName}'
  location: 'eastasia'
  tags: union(tags, { 'azd-service-name': 'web' })
  sku: {
    name: 'Free'
    tier: 'Free'
  }
  properties: {
    buildProperties: {
      skipGithubActionWorkflowGeneration: true
    }
  }
}

output apiUri string = 'https://${app.properties.configuration.ingress.fqdn}'
output AZURE_CONTAINER_REGISTRY_ENDPOINT string = containerRegistry.properties.loginServer
output AUTH_CLIENT_ID string = apiApp.appId
output AUTH_SCOPE string = 'api://opportunity-engineering-${environmentName}/access_as_user'
output FACILITATOR_GROUP_ID string = facilitatorGroup.id
output WEB_URI string = 'https://${staticWebApp.properties.defaultHostname}'
output STATIC_WEB_APP_NAME string = staticWebApp.name
