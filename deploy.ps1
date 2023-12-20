param (
    [Parameter(Mandatory = $True)]
    [string]
    $resourceGroup, 
    [Parameter(Mandatory = $True)]
    [string]
    $location,
    [Parameter(Mandatory = $True)]
    [string]
    $translationAcct,
    [Parameter(Mandatory = $True)]
    [string]
    $storageAcct
)
$error.Clear()
$ErrorActionPreference = 'Stop'


Write-Host -ForegroundColor Green "Creating resource group $resourceGroup and required resources in $location"
$result = az deployment sub create --location $location --template-file .\infra\main.bicep --parameters resourceGroupName=$resourceGroup storageAccountName=$storageAcct translateAccountName=$translationAcct location=$location | ConvertFrom-Json

Write-Host -ForegroundColor Green "Deployment Result"
Write-Host -ForegroundColor Yellow ($result.properties.outputs | ConvertTo-Json)

if(!$?){ exit }

Write-Host -ForegroundColor Green "Getting translation account account key"
$translatorKey = az cognitiveservices account keys list --resource-group $resourceGroup  --name $translationAcct -o tsv --query key1


 $localsettings = @{
        "SOURCE_BLOB_SAS" = $result.properties.outputs.incoming_url.value
        "TARGET_BLOB_SAS" = $result.properties.outputs.translated_url.value
        "TRANSLATOR_TEXT_SUBSCRIPTION_KEY" = $translatorKey
        "TRANSLATOR_DOCUMENT_ENDPOINT" =  $result.properties.outputs.translatorDocEndpoint.value
        "TRANSLATOR_TEXT_ENDPOINT" = "https://api.cognitive.microsofttranslator.com/" #$result.properties.outputs.translatorEndpoint.value
        "TRANSLATOR_SERVICE_REGION" = $result.properties.outputs.location.value
        "CUSTOM_TRANSLATOR_CATEGORY_ID" = ""
        "CUSTOM_TRANSLATOR_TO_LANGUAGE" = ""
}
       

Write-Host -ForegroundColor Green "Creating local.settings.json"
$localsettingsJson = ConvertTo-Json $localsettings -Depth 100
$localsettingsJson | Out-File -FilePath ".\local.settings.json"

Write-Host -ForegroundColor Green "Running app..."
dotnet build --no-incremental .\translator_demo.csproj -o .\bin\demo

.\bin\demo\translator_demo.exe





