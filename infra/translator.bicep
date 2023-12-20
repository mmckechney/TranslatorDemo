param translateAccountName string
param location string = resourceGroup().location

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

output translatorTextEndpoint string = translationServiceResource.properties.endpoints.textTranslation
output translatorDocEndpoint string = translationServiceResource.properties.endpoints.documentTranslation
output location string = translationServiceResource.location
