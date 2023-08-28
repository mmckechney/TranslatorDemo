[CmdletBinding()]
param (
    $resourceGroup, 
    $translationAcct,
    $storageAcct
)
$error.Clear()
$ErrorActionPreference = 'Stop'


$result =  az deployment group create --template-file .\template.bicep --parameters .\parameters.json --resource-group $resourceGroup | ConvertFrom-Json


$incomingUrl = $result.properties.outputs.incoming_url
$translatedUrl = $result.properties.outputs.translated_url
$translatorKey = $result.properties.outputs.translatorKey
$translatorDocEndpoint = $result.properties.outputs.translatorDocEndpoint
$location = $result.properties.outputs.location

$launchSettings = @{
    "profiles" = @{
        "translator_demo" = @{
            "commandName" = "Project"
            "environmentVariables" = @{
                "SOURCE_BLOB_SAS" = $incomingUrl.Value
                "TARGET_BLOB_SAS" = $translatedUrl.Value
                "TRANSLATOR_TEXT_SUBSCRIPTION_KEY" = $translatorKey.Value
                "TRANSLATOR_DOCUMENT_ENDPOINT" = $translatorDocEndpoint.Value
                "TRANSLATOR_TEXT_ENDPOINT" = "https://api.cognitive.microsofttranslator.com/"
                "TRANSLATOR_SERVICE_REGION" = $location.Value
            }
        }
    }
}

$launchSettingsJson = ConvertTo-Json $launchSettings -Depth 100
Write-Host $launchSettingsJson

$launchSettingsJson | Out-File -FilePath "..\Properties\launchSettings.json"


