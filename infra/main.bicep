targetScope = 'subscription'

@minLength(1)
@description('Primary location for all resources')
param location string
param resourceGroupName string
param storageAccountName string
param translateAccountName string


resource rg 'Microsoft.Resources/resourceGroups@2022-09-01' = {
    name: resourceGroupName
    location: location
}

module storage 'storage.bicep' = {
    name: 'storage'
    scope: resourceGroup(resourceGroupName)
    params: {
        location: location
        storageAccountName:storageAccountName
      
    }
    dependsOn: [
        rg
    ]
}

module translator 'translator.bicep' = {
	scope: resourceGroup(resourceGroupName)
	name: 'translator'
	params: {
        location: location
		translateAccountName: translateAccountName
	}
    dependsOn: [
        rg
    ]
}

output translatorEndpoint string = translator.outputs.translatorTextEndpoint
output translatorDocEndpoint string = translator.outputs.translatorDocEndpoint
output location string = translator.outputs.location
output translated_url string = storage.outputs.translated_url
output incoming_url string = storage.outputs.incoming_url
