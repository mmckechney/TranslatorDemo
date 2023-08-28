param storageAccountName string 
param translateAccountName string
param location string = resourceGroup().location
param sasStartDate string =  utcNow('u')
resource translationServiceResource 'Microsoft.CognitiveServices/accounts@2023-05-01' = {
  name: translateAccountName
  location: location
  sku: {
    name: 'S1'
  }
  kind: 'TextTranslation'
  identity: {
    type: 'None'
  }
  properties: {
    customSubDomainName: translateAccountName
    networkAcls: {
      defaultAction: 'Allow'
      virtualNetworkRules: []
      ipRules: []
    }
    publicNetworkAccess: 'Enabled'
  }
}

resource storageAccountResource  'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: storageAccountName
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    dnsEndpointType: 'Standard'
    defaultToOAuthAuthentication: false
    publicNetworkAccess: 'Enabled'
    allowCrossTenantReplication: true
    minimumTlsVersion: 'TLS1_2'
    allowBlobPublicAccess: false
    allowSharedKeyAccess: true
    networkAcls: {
      bypass: 'AzureServices'
      virtualNetworkRules: []
      ipRules: []
      defaultAction: 'Allow'
    }
    supportsHttpsTrafficOnly: true
    encryption: {
      requireInfrastructureEncryption: false
      services: {
        file: {
          keyType: 'Account'
          enabled: true
        }
        blob: {
          keyType: 'Account'
          enabled: true
        }
      }
      keySource: 'Microsoft.Storage'
    }
    accessTier: 'Hot'
  }
}

resource storageAccount_blobService 'Microsoft.Storage/storageAccounts/blobServices@2023-01-01' = {
  parent: storageAccountResource
  name: 'default'
  properties: {
    changeFeed: {
      enabled: false
    }
    restorePolicy: {
      enabled: false
    }
    containerDeleteRetentionPolicy: {
      enabled: true
      days: 7
    }
    cors: {
      corsRules: []
    }
    deleteRetentionPolicy: {
      allowPermanentDelete: false
      enabled: true
      days: 7
    }
    isVersioningEnabled: false
  }
}

resource storageContainer_incoming 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-01-01' = {
  parent: storageAccount_blobService
  name: 'incoming'
  properties: {
    immutableStorageWithVersioning: {
      enabled: false
    }
    defaultEncryptionScope: '$account-encryption-key'
    denyEncryptionScopeOverride: false
    publicAccess: 'None'
  }
}

resource storageContainer_translated 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-01-01' = {
  parent: storageAccount_blobService
  name: 'translated'
  properties: {
    immutableStorageWithVersioning: {
      enabled: false
    }
    defaultEncryptionScope: '$account-encryption-key'
    denyEncryptionScopeOverride: false
    publicAccess: 'None'
  }
 
}

var incoming_sas = listServiceSas(storageAccountResource.name, '2021-04-01',
      {
        canonicalizedResource: '/blob/${storageAccountResource.name}/${storageContainer_incoming.name}'
        signedResource: 'c'
        signedProtocol: 'https'
        signedPermission: 'rwl'
        signedServices: 'b'
        signedExpiry: dateTimeAdd(sasStartDate, 'P1Y')
      }).serviceSasToken

output incoming_url string = '${storageAccountResource.properties.primaryEndpoints.blob}${storageContainer_incoming.name}?${incoming_sas}'

var translated_sas = listServiceSas(storageAccountResource.name, '2021-04-01',
      {
        canonicalizedResource: '/blob/${storageAccountResource.name}/${storageContainer_translated.name}'
        signedResource: 'c'
        signedProtocol: 'https'
        signedPermission: 'rwl'
        signedServices: 'b'
        signedExpiry: dateTimeAdd(sasStartDate, 'P1Y')
      }).serviceSasToken

output translated_url string = '${storageAccountResource.properties.primaryEndpoints.blob}${storageContainer_translated.name}?${translated_sas}'


      #disable-next-line outputs-should-not-contain-secrets
output translatorKey string = translationServiceResource.listKeys().key1

output translatorTextEndpoint string = translationServiceResource.properties.endpoints.textTranslation
output translatorDocEndpoint string = translationServiceResource.properties.endpoints.documentTranslation
output location string = translationServiceResource.location
