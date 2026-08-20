targetScope = 'subscription'

@minLength(3)
@maxLength(20)
param environmentName string

@allowed([
  'australiaeast'
])
param location string = 'australiaeast'

@description('Immutable container image reference, preferably pinned by digest.')
param containerImage string

@secure()
param entraTenantId string

param foundryModelDeploymentName string = 'gpt-4o'
param foundryModelIdentity string = 'gpt-4o'
param foundryModelName string = 'gpt-4o'
param foundryModelVersion string = '2024-11-20'
param guardrailMode string = 'evaluation-only'
param guardrailApprovedBy string = 'deployment-approval-required'
param guardrailRollbackReference string = 'initial-policy'
param fabricGovernance object = {
  Requested: false
  CapacityReady: false
  TenantAiSettingsReady: false
  WorkspaceIdentityReady: false
  OneLakeSecurityReady: false
  AuditReady: false
  DirectLakeUsed: false
  QueryModeObservation: 'not-validated'
  ApprovedDatasets: []
}

var resourceGroupName = 'rg-${environmentName}-${location}'
var tags = {
  'Az.Project.Environment::Value': environmentName
  'Az.Project.Region::Value': location
  'Az.Project.ManagedBy::Value': 'azd'
  'Az.Data.Classification::Value': 'Confidential'
}

resource resourceGroup 'Microsoft.Resources/resourceGroups@2025-04-01' = {
  name: resourceGroupName
  location: location
  tags: tags
}

module application 'modules/application.bicep' = {
  name: 'opportunity-engineering-${environmentName}'
  scope: resourceGroup
  params: {
    environmentName: environmentName
    location: location
    tags: tags
    containerImage: containerImage
    entraTenantId: entraTenantId
    foundryModelDeploymentName: foundryModelDeploymentName
    foundryModelIdentity: foundryModelIdentity
    foundryModelName: foundryModelName
    foundryModelVersion: foundryModelVersion
    guardrailMode: guardrailMode
    guardrailApprovedBy: guardrailApprovedBy
    guardrailRollbackReference: guardrailRollbackReference
    fabricGovernance: fabricGovernance
  }
}

output AZURE_RESOURCE_GROUP string = resourceGroup.name
output AZURE_CONTAINER_REGISTRY_ENDPOINT string = application.outputs.AZURE_CONTAINER_REGISTRY_ENDPOINT
output API_URI string = application.outputs.apiUri
output AUTH_CLIENT_ID string = application.outputs.AUTH_CLIENT_ID
output AUTH_SCOPE string = application.outputs.AUTH_SCOPE
output FACILITATOR_GROUP_ID string = application.outputs.FACILITATOR_GROUP_ID
output WEB_URI string = application.outputs.WEB_URI
output STATIC_WEB_APP_NAME string = application.outputs.STATIC_WEB_APP_NAME
