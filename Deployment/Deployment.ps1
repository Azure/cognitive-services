# Initial settings required to kick things off. Your subscription ID, location
# and resource group name.

#######################################################################
#>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
#>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
# USER REQUIRED - Subscription Level Information
#>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
#>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
#######################################################################
$subscriptionId = "XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX (YOUR_SUBSCRIPTION_ID)"
$resourceGroupName="YOUR_RESOURCE_GROUP_NAME"
$locationString = "eastus"


#######################################################################
#>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
#>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
# Remaining variables, OK not to change
#>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
#>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
#######################################################################


#######################################################################
# Storage account for images from documents.
#######################################################################
$storageAccountDeploymentName = "stgcreation"
$storageAccountType = "Standard_LRS"
$storageAccountName = "rssimages"

#Set up parameters to create storage account.
$storageCreateParameters = @{}
$storageCreateParameters.Add("location", $locationString)
$storageCreateParameters.Add("storageAccountType",$storageAccountType)
$storageCreateParameters.Add("storageAccountName",$storageAccountName)

#######################################################################
# Vision API Cognitive Service
#######################################################################
$computerVisionDeploymentName = "visioncreation"
$computerVisionAccountName = "VisionAPI"
$computerVisionSKU = "S1"

#Set up parameters to create storage account.
$visionApiCreateParameters = @{}
$visionApiCreateParameters.Add("accountName", $computerVisionAccountName)
$visionApiCreateParameters.Add("SKU",$computerVisionSKU)
$visionApiCreateParameters.Add("cognitiveServicesLocation",$locationString)


#######################################################################
# Translation API Cognitive Service
#######################################################################
$translationDeploymentName = "translationcreation"
$translationAccountName = "TranslationAPI"
$translationSKU = "S1"

#Set up parameters to create storage account.
$translationApiCreateParameters = @{}
$translationApiCreateParameters.Add("accountName", $translationAccountName)
$translationApiCreateParameters.Add("SKU",$translationSKU)

#######################################################################
# Face API Cognitive Service
#######################################################################
$faceDeploymentName = "facecreation"
$faceAccountName = "FaceAPI"
$faceSKU = "S0"

#Set up parameters to create storage account.
$faceApiCreateParameters = @{}
$faceApiCreateParameters.Add("accountName", $faceAccountName)
$faceApiCreateParameters.Add("SKU",$faceSKU)
$faceApiCreateParameters.Add("cognitiveServicesLocation",$locationString)

#######################################################################
# Text API Cognitive Service
#######################################################################
$textDeploymentName = "textcreation"
$textAccountName = "TextAPI"
$textSKU = "S0"

#Set up parameters to create storage account.
$textApiCreateParameters = @{}
$textApiCreateParameters.Add("accountName", $textAccountName)
$textApiCreateParameters.Add("SKU",$textSKU)
$textApiCreateParameters.Add("cognitiveServicesLocation",$locationString)

#######################################################################
# CosmosDB
#######################################################################
$cosmosDeploymentName = "comsoscreate"
$cosmosApiType = "SQL"
$cosmosAccountName = "cosmos"
$cosmosDatabase = "Articles"
$cosmosInspectionCollection = "Inspection"
$cosmosProcessedCollection = "Processed"
$cosmosIngestCollection = "Ingest"

#Set up parameters to create CosmosDB account.
$cosmosCreateParameters = @{}
$cosmosCreateParameters.Add("apiType", $cosmosApiType)
$cosmosCreateParameters.Add("databaseAccountName",$cosmosAccountName)
$cosmosCreateParameters.Add("location",$locationString)

#######################################################################
# Service Bus
#######################################################################
$serviceBusDeploymentName = "servicebuscreate"
$serviceBusNamespace = "cosmossb"
$translationQueue = "translationqueue"
$ocrQueue = "ocrqueue"
$faceQueue = "faceapiqueue"
$inspectionQueue = "inspectionqueue"

#Set up parameters to create CosmosDB account.
$serviceBusCreateParameters = @{}
$serviceBusCreateParameters.Add("serviceBusNamespaceName", $serviceBusNamespace)
$serviceBusCreateParameters.Add("serviceBusTranslationQueueName",$translationQueue)
$serviceBusCreateParameters.Add("serviceBusOCRQueueName",$ocrQueue)
$serviceBusCreateParameters.Add("serviceBusFaceQueueName",$faceQueue)
$serviceBusCreateParameters.Add("serviceBusInspectionQueueName",$inspectionQueue)
$serviceBusCreateParameters.Add("location",$locationString)

#######################################################################
# Function App
#######################################################################
$fnAppDeploymentName = "functionappcreate"
$fnAppName = "cosmosfn"
$fnStgType = "Standard_LRS"
$fnAppVersion = "2015-08-01"
$fnAppType = "Microsoft.Web/sites"


#Set up parameters to create CosmosDB account.
$fnAppCreateParameters = @{}
$fnAppCreateParameters.Add("appName", $fnAppName)
$fnAppCreateParameters.Add("storageAccountType",$fnStgType)
$fnAppCreateParameters.Add("location",$locationString)


#######################################################################
#>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
#>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
# ACTIVITY
#>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
#>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
#######################################################################


#######################################################################
# Log in and Select the right subscription.
#######################################################################
Login-AzureRMAccount
Select-AzureRmSubscription -SubscriptionId $subscriptionId


#######################################################################
# Create the resource group.
#######################################################################
Write-Host("Creating resource group")
New-AzureRmResourceGroup -Name $resourceGroupName -Location $locationString


#######################################################################
# Create the storage account used to hold image/videos/azure functions
#######################################################################
Write-Host("Creating additional storage account for images/functions.")
New-AzureRmResourceGroupDeployment -Name $storageAccountDeploymentName -ResourceGroupName $resourceGroupName -TemplateFile ".\StorageAccount.json" -TemplateParameterObject $storageCreateParameters
$additionalStorageAccountInfo = @{}
$additionalStorageAccountInfo.Add("storageKey", (Get-AzureRmResourceGroupDeployment -ResourceGroupName $resourceGroupName -Name $storageAccountDeploymentName).Outputs.storageKey.value)
$additionalStorageAccountInfo.Add("accountName", (Get-AzureRmResourceGroupDeployment -ResourceGroupName $resourceGroupName -Name $storageAccountDeploymentName).Outputs.storageName.value)
$additionalStorageAccountInfo.Add("connectionString", "DefaultEndpointsProtocol=https;AccountName=" + $additionalStorageAccountInfo["accountName"] + ";AccountKey=" +  $additionalStorageAccountInfo["storageKey"])

# Copy the website files to storage
Write-Host("Uploading function zip.")
$functionsContainerName = "acurefunctions"
$websiteFile = "AzureFunctions.zip"
$websitePackage = ".\Functions\AzureFunctions.zip"
$websitePackageLocation = "https://" + $additionalStorageAccountInfo["accountName"] + ".blob.core.windows.net/" + $functionsContainerName + "/" + $websiteFile

# create a context for account and key
$ctx = New-AzureStorageContext $additionalStorageAccountInfo["accountName"] $additionalStorageAccountInfo["storageKey"]

New-AzureStorageContainer -Name $functionsContainerName -Context $ctx -Permission blob
Set-AzureStorageBlobContent -File $websitePackage -Container $functionsContainerName -Blob $websiteFile -Context $ctx 


#######################################################################
# Create computer vision API service
#######################################################################
Write-Host("Creating Computer Vision API")
New-AzureRmResourceGroupDeployment -Name $computerVisionDeploymentName -ResourceGroupName $resourceGroupName -TemplateFile ".\ComputerVision.json" -TemplateParameterObject $visionApiCreateParameters
$computerVisionAccountInfo = @{}
$computerVisionAccountInfo.Add("apiKey", (Get-AzureRmResourceGroupDeployment -ResourceGroupName $resourceGroupName -Name $computerVisionDeploymentName).Outputs.cognitivekey.value)
$computerVisionAccountInfo.Add("endpoint", (Get-AzureRmResourceGroupDeployment -ResourceGroupName $resourceGroupName -Name $computerVisionDeploymentName).Outputs.endpoint.value)

#######################################################################
# Create translation API service
#######################################################################
Write-Host("Creating Translation API")
New-AzureRmResourceGroupDeployment -Name $translationDeploymentName -ResourceGroupName $resourceGroupName -TemplateFile ".\Translate.json" -TemplateParameterObject $translationApiCreateParameters
$translationAccountInfo = @{}
$translationAccountInfo.Add("apiKey", (Get-AzureRmResourceGroupDeployment -ResourceGroupName $resourceGroupName -Name $translationDeploymentName).Outputs.cognitivekey.value)
$translationAccountInfo.Add("endpoint", (Get-AzureRmResourceGroupDeployment -ResourceGroupName $resourceGroupName -Name $translationDeploymentName).Outputs.endpoint.value)
$translationAccountInfo.Add("globalEndpoint", "https://api.cognitive.microsofttranslator.com")

#######################################################################
# Create Face API service
#######################################################################
Write-Host("Creating Face API")
New-AzureRmResourceGroupDeployment -Name $faceDeploymentName -ResourceGroupName $resourceGroupName -TemplateFile ".\Face.json" -TemplateParameterObject $faceApiCreateParameters
$faceAccountInfo = @{}
$faceAccountInfo.Add("apiKey", (Get-AzureRmResourceGroupDeployment -ResourceGroupName $resourceGroupName -Name $faceDeploymentName).Outputs.cognitivekey.value)
$faceAccountInfo.Add("endpoint", (Get-AzureRmResourceGroupDeployment -ResourceGroupName $resourceGroupName -Name $faceDeploymentName).Outputs.endpoint.value)

#######################################################################
# Create Text API service
#######################################################################
Write-Host("Creating Text Analytics API")
New-AzureRmResourceGroupDeployment -Name $textDeploymentName -ResourceGroupName $resourceGroupName -TemplateFile ".\Text.json" -TemplateParameterObject $textApiCreateParameters
$textAccountInfo = @{}
$textAccountInfo.Add("apiKey", (Get-AzureRmResourceGroupDeployment -ResourceGroupName $resourceGroupName -Name $textDeploymentName).Outputs.cognitivekey.value)
$textAccountInfo.Add("endpoint", (Get-AzureRmResourceGroupDeployment -ResourceGroupName $resourceGroupName -Name $textDeploymentName).Outputs.endpoint.value)

#######################################################################
# Create CosmosDB
#######################################################################
Write-Host("Creating CosmosDB")
New-AzureRmResourceGroupDeployment -Name $cosmosDeploymentName -ResourceGroupName $resourceGroupName -TemplateFile ".\Cosmos.json" -TemplateParameterObject $cosmosCreateParameters
$cosmosAccountInfo = @{}
$cosmosAccountInfo.Add("apiKey", (Get-AzureRmResourceGroupDeployment -ResourceGroupName $resourceGroupName -Name $cosmosDeploymentName).Outputs.cosmoskey.value)
$cosmosAccountInfo.Add("endpoint", (Get-AzureRmResourceGroupDeployment -ResourceGroupName $resourceGroupName -Name $cosmosDeploymentName).Outputs.endpoint.value)
$cosmosAccountInfo.Add("name", (Get-AzureRmResourceGroupDeployment -ResourceGroupName $resourceGroupName -Name $cosmosDeploymentName).Outputs.cosmosname.value)
$cosmosAccountInfo.Add("connectionString", "AccountEndpoint=" + $cosmosAccountInfo["endpoint"] + ";AccountKey=" +  $cosmosAccountInfo["apiKey"] + ";")

#######################################################################
#>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
#>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
# Create Configuration file for RssGenerator application and seed 
# CosmosDB with database and collections.
#>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
#>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
#######################################################################
Write-Host("Creating configuration file for RssGenerator Application")

$configurationCollections = @($cosmosIngestCollection,$cosmosProcessedCollection,$cosmosInspectionCollection)

$nasa = @{}
$nasa.Add("storage_container","nasa")
$nasa.Add("feed","https://www.nasa.gov/rss/dyn/breaking_news.rss")
$quebec = @{}
$quebec.Add("storage_container","russiangov")
$quebec.Add("feed","http://government.ru/all/rss/")
$germany = @{}
$germany.Add("storage_container","bamf")
$germany.Add("feed","http://www.bamf.de/SiteGlobals/Functions/RSS/DE/Feed/RSSNewsfeed_Meldungen")
$configurationFeeds = @($nasa, $quebec, $germany)

$configuration = @{}
$configuration.Add("cosmos_db_uri",$cosmosAccountInfo["endpoint"])
$configuration.Add("cosmos_db_key",$cosmosAccountInfo["apiKey"])
$configuration.Add("cosmos_db_database",$cosmosDatabase)
$configuration.Add("cosmos_db_ingest_collection",$cosmosIngestCollection)
$configuration.Add("cosmos_db_collections",$configurationCollections)
$configuration.Add("azure_storage_connection",$additionalStorageAccountInfo["connectionString"])
$configuration.Add("rss_feeds",$configurationFeeds)

$configuration | ConvertTo-Json -depth 100 | Out-File ".\RssGenerator\Configuration.json"
Write-Host("Seeding CosmosDB with Database and Collections")
.\RssGenerator\RssGenerator seed

#######################################################################
# Create Service Bus and Queues
#######################################################################
Write-Host("Creating Service Bus and Queues")
New-AzureRmResourceGroupDeployment -Name $serviceBusDeploymentName -ResourceGroupName $resourceGroupName -TemplateFile ".\ServiceBus.json" -TemplateParameterObject $serviceBusCreateParameters

$serviceBusInfo = @{}
$serviceBusInfo.Add("connectionstring", (Get-AzureRmResourceGroupDeployment -ResourceGroupName $resourceGroupName -Name $serviceBusDeploymentName).Outputs.namespaceConnectionString.value)
$serviceBusInfo.Add("primarykey", (Get-AzureRmResourceGroupDeployment -ResourceGroupName $resourceGroupName -Name $serviceBusDeploymentName).Outputs.sharedAccessPolicyPrimaryKey.value)
$serviceBusInfo.Add("name", (Get-AzureRmResourceGroupDeployment -ResourceGroupName $resourceGroupName -Name $serviceBusDeploymentName).Outputs.namespaceName.value)
$serviceBusInfo.Add("translationQueue",$translationQueue)
$serviceBusInfo.Add("ocrQueue",$ocrQueue)
$serviceBusInfo.Add("faceQueue",$faceQueue)
$serviceBusInfo.Add("inspectionQueue",$inspectionQueue)

#######################################################################
# Create Function App
#######################################################################
Write-Host("Creating Function App")

$fnAppCreateParameters.Add("cosmosConnectionString",$cosmosAccountInfo["connectionString"])
$fnAppCreateParameters.Add("cosmosDatabase",$cosmosDatabase)
$fnAppCreateParameters.Add("cosmosInspectionCollection", $cosmosInspectionCollection)
$fnAppCreateParameters.Add("cosmosProcessedCollection",$cosmosProcessedCollection)
$fnAppCreateParameters.Add("cosmosIngestCollection",$cosmosIngestCollection)
$fnAppCreateParameters.Add("sbConnectionString",$serviceBusInfo["connectionstring"])
$fnAppCreateParameters.Add("faceKey",$faceAccountInfo["apiKey"])
$fnAppCreateParameters.Add("faceURI",$faceAccountInfo["endpoint"])
$fnAppCreateParameters.Add("textKey",$textAccountInfo["apiKey"])
$fnAppCreateParameters.Add("textURI",$textAccountInfo["endpoint"])
$fnAppCreateParameters.Add("translationKey",$translationAccountInfo["apiKey"])
$fnAppCreateParameters.Add("translationURI",$translationAccountInfo["globalEndpoint"])
$fnAppCreateParameters.Add("translationLang","en")
$fnAppCreateParameters.Add("visionKey",$computerVisionAccountInfo["apiKey"])
$fnAppCreateParameters.Add("visionURI",$computerVisionAccountInfo["endpoint"])
$fnAppCreateParameters.Add("packageUri",$websitePackageLocation)

New-AzureRmResourceGroupDeployment -Name $fnAppDeploymentName -ResourceGroupName $resourceGroupName -TemplateFile ".\FunctionApp.json" -TemplateParameterObject $fnAppCreateParameters

$functionAppInfo = @{}
$functionAppInfo.Add("storageKey", (Get-AzureRmResourceGroupDeployment -ResourceGroupName $resourceGroupName -Name $fnAppDeploymentName).Outputs.storageKey.value)
$functionAppInfo.Add("storageName", (Get-AzureRmResourceGroupDeployment -ResourceGroupName $resourceGroupName -Name $fnAppDeploymentName).Outputs.storageName.value)
$functionAppInfo.Add("fnappname", (Get-AzureRmResourceGroupDeployment -ResourceGroupName $resourceGroupName -Name $fnAppDeploymentName).Outputs.functionAppName.value)
$functionAppInfo.Add("stgConnectionString", "DefaultEndpointsProtocol=https;AccountName=" + $functionAppInfo["storageName"] + ";AccountKey=" +  $functionAppInfo["storageKey"])

Write-Host("Force function app to refresh for Cosmos DB connection")

# Forcing the funciton app *should* cause the trigger function to be refreshed, but it doesn't due to an issue.
# Invoke-AzureRmResourceAction -ResourceGroupName $resourceGroupName -ResourceType $fnAppType -ResourceName $functionAppInfo["fnappname"] -Action syncfunctiontriggers -ApiVersion $fnAppVersion -Force

# You must ping the endpoint to get the function app to make the connections needed
$fnAppTrigger= "https://" + $functionAppInfo["fnappname"] + ".azurewebsites.net"
$triggerRequest = [System.Net.WebRequest]::Create($fnAppTrigger)
$triggerResponse = $triggerRequest.GetResponse()
Write-Host($triggerResponse)
$triggerStatus = [int]$triggerResponse.StatusCode
Write-Host($triggerStatus)

#######################################################################
#>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
#>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
# OUTPUTS - The following is a collection of output values from the 
#			above deployments. If interested, uncomment these lines to 
#			view those values.
#>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
#>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
#######################################################################

#Write-Host ("Additional Storage Data")
#Write-Host ( $additionalStorageAccountInfo["storageKey"])
#Write-Host ( $additionalStorageAccountInfo["accountName"])
#Write-Host ( $additionalStorageAccountInfo["connectionString"])

#Write-Host ("Computer Vision Data")
#Write-Host ( $computerVisionAccountInfo["apiKey"])
#Write-Host ( $computerVisionAccountInfo["endpoint"])

#Write-Host ("Translation Data")
#Write-Host ( $translationAccountInfo["apiKey"])
#Write-Host ( $translationAccountInfo["endpoint"])

##Global is the one to use in the web app
#Write-Host ( $translationAccountInfo["globalEndpoint"])

#Write-Host ("Face Data")
#Write-Host ( $faceAccountInfo["apiKey"])
#Write-Host ( $faceAccountInfo["endpoint"])

#Write-Host ("Text Data")
#Write-Host ( $textAccountInfo["apiKey"])
#Write-Host ( $textAccountInfo["endpoint"])

#Write-Host ("Cosmos DB")
#Write-Host ( $cosmosAccountInfo["apiKey"])
#Write-Host ( $cosmosAccountInfo["endpoint"])
#Write-Host ( $cosmosAccountInfo["name"])
## ArticleIngestTrigger_ConnectionString
#Write-Host ( $cosmosAccountInfo["connectionString"])

#Write-Host ("Service bus")
#Write-Host ( $serviceBusInfo["connectionstring"])
#Write-Host ( $serviceBusInfo["primarykey"])
#Write-Host ( $serviceBusInfo["name"])
#Write-Host ( $serviceBusInfo["translationQueue"])
#Write-Host ( $serviceBusInfo["ocrQueue"])
#Write-Host ( $serviceBusInfo["faceQueue"])
#Write-Host ( $serviceBusInfo["inspectionQueue"])

#Write-Host ("Function App")
#Write-Host ( $functionAppInfo["storageKey"])
#Write-Host ( $functionAppInfo["storageName"])
#Write-Host ( $functionAppInfo["fnappname"])
#Write-Host ( $functionAppInfo["stgConnectionString"])

#######################################################################
#>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
#>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
# Additional Instructions
#>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
#>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
#######################################################################
# Example Location : https://cosmosfnappba4e.file.core.windows.net/cosmosfnappba4e/site/wwwroot/
Write-Host("")
Write-Host("Configuration of this solution is almost completed, the Azure Function Apps need to be launched ")
Write-Host("to make the connection to Cosmos DB.")
Write-Host("1. Navigate to the azure portal https://portal.azure.com and find the resource group " + $resourceGroupName)
Write-Host("2. Launch the app service " + $functionAppInfo["fnappname"] + " that hosts the function app within that resource group")
Write-Host("3. Click on the function ArticleIngestTrigger to make the connection to Cosmos DB.")
Write-Host("")
Write-Host("When that is completed, the next step is to seed the Cosmos DB Collection that will fire")
Write-Host("off the pipeline. Launch the following application from the Deployment directory:")
Write-Host("Application : /Deployment/RssGenerator/RssGenerator.exe")
Write-Host("")
