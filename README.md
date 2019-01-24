# Demos - Cognitive Services
<sup>Use Case: Mass Ingestion of Electronic Documents</sup> <br>
<sup>Created by Dan Grecoe, a Microsoft employee</sup>

There are quite a few Azure services that can be used right out of the box to provide Machine Learning and Artificial Intelligence in the [Azure Cognitive Services](https://azure.microsoft.com/en-us/services/cognitive-services/) suite. There are text, computer vision, facial recognition, video indexing, etc. services that offer some powerful functionality without you, the user, ever having to write a line of code or understand the machine learning concepts that underpin them. 

However, it would be potentially too confusing to create an example that uses each and every feature, with the permutations, in each and every available cognitive service so this demo has hand picked a few services to use in a coherent way.

So, you ask, what is this demo about? In short, this demo shows how to set up a pipeline for mass ingestion and near-realtime analysis of electronic documents to provide useful insights as the documents are being stored into durable storage.

To simplify the concept, this demo will read public RSS news feeds from three different organizations in three different languages. This demo could easily be extended to include more RSS feeds by simply changing the configuration of the RSS feed application (more on that later). The example feeds used in this demo are:

Language  | RSS Feed
---- | ----
English | https://www.nasa.gov/rss/dyn/breaking_news.rss
Russian | http://government.ru/all/rss/ 
German | http://www.bamf.de/SiteGlobals/Functions/RSS/DE/Feed/RSSNewsfeed_Meldungen

The "requirements" for this project are that, for each document ingested into the pipeline, it must:
1. Translate all text found in any article to English (though the target language should be, and is, programmable).

2. Find key phrases in the text and document them.
3. Find known entities by full name or abbreviation, and provide information about those known entities.
4. Detect sentiment (positive, negative or neutral) from both the title and body text.
5. Detect text in any associated image and document what is found.
6. Detect objects in any associated image and document what is found. 
7. Detect people in any associated image and document people count, and for each person, document gender and estimated age. 

> <b> NOTE: </b> While it would be quite useful to use the Text Analytics on the text stripped out of images, it has been left out of this example for simplicity.

Without Azure Cognitive Services, you'd probably be scratching your head on how to accomplish these tasks. However, the base cognitive services offered in Azure provide these services directly out of the box. Specifically, the services that will be utilized are:

#### Azure Cognitive Services used in this demo
Cognitive Service | Purpose
:---------------------:| --- 
[Translation API](https://azure.microsoft.com/en-us/services/cognitive-services/translator-text-api/) | Determines the language of the incoming title and body, when present, then translates them to English. However, the target language is just another input and can be changed from English to any [supported language](https://docs.microsoft.com/en-us/azure/cognitive-services/translator/reference/v3-0-languages) of your choice.
[Text Analytics](https://azure.microsoft.com/en-us/services/cognitive-services/text-analytics/) | Used to find <i>key word phrases</i> and <i>entities</i> in title and body text after it has been translated.
[Computer Vision](https://azure.microsoft.com/en-us/services/cognitive-services/computer-vision/)|Inspects each image associated with an incoming article to (1) scrape out written words from the image and (2) determine what types of objects are present in the image. 
[Face API](https://azure.microsoft.com/en-us/services/cognitive-services/face/)|Inspects each image associated with an incoming article to find faces and determine whether the face represents a male or female and associates an estimated age to those faces.

#### Other Azure Services 
Azure Service | Purpose
:---------------------:| --- 
[Azure Storage](https://azure.microsoft.com/en-us/services/storage/)|Holds images from articles and hosts the code for the Azure Functions.
[Azure Cosmos DB](https://azure.microsoft.com/en-us/services/cosmos-db/)| NoSQL database where original content as well as processing results are stored.
[Azure Service Bus](https://azure.microsoft.com/en-us/services/service-bus/)|Service bus queues are used as triggers for durable Azure Functions.
[Azure Functions](https://azure.microsoft.com/en-us/try/app-service/)|Code blocks that analyze the documents stored in the Azure Cosmos DB.

> <b> Note </b> This design uses the queue notification/Azure function pattern for simplicity. While this is a solid pattern, this is not the only pattern that can be used to accomplish this data flow.
>
> [Azure Service Bus Topics](https://docs.microsoft.com/en-us/azure/service-bus-messaging/service-bus-dotnet-how-to-use-topics-subscriptions) could be used which would allow processing different parts of the article in a parallel as opposed to the serial processing done in this example. Topics would be useful if article inspection processing time is critical.  A comparison between Azure Service Bus Queues and Azure Service Bus Topics can be found [here](https://docs.microsoft.com/en-us/azure/service-bus-messaging/service-bus-dotnet-how-to-use-topics-subscriptions).
>
>Azure functions could also be implemented in an [Azure Logic App](https://azure.microsoft.com/en-us/services/logic-apps/).  However, with parallel processing the user would have to implement record-level locking such as [Redlock](https://redis.io/topics/distlock) until Cosmos DB supports [partial document updates](https://feedback.azure.com/forums/263030-azure-cosmos-db/suggestions/6693091-be-able-to-do-partial-updates-on-document). 
>
>A comparison between durable functions and Logic apps can be found [here](https://docs.microsoft.com/en-us/azure/azure-functions/functions-compare-logic-apps-ms-flow-webjobs).
>
> Finally, all of the AI types introduced in this article are out-of-the-box services provided by Azure. There is nothing in this architecture that prevents an implementation that utilizes customized AI components in this process.  

#### Pipeline Architecture / Data Flow
<center>
<img src=".\images\pipeline.png">
</center>

For record data formats see [\RssGenerator\README.md](./RssGenerator)

This demo is a series of Azure Services stitched together to process digital articles that contain media (images) in an automated fashion.

The generator in this case is contained in this repository and named RSSGenerator. This generator is used to populate a Cosmos DB Collection with RSS articles in multiple languages which then trigger the following actions: 

* Note that there can be many generators/ingestion processes feeding the CosmosDB and Azure Storage Account.

1. Ingest function is notified on document inserts into the CosmosDB database in the Ingest collection.
    -	Image(s) are stored in an Azure Storage Account as blobs during the ingest process.
    -  Notifications can be for one or more inserts.
    - Inserts can be of type article or image where image documents are associated with the originating article.
    -	Image documents are inserted before the article record as the Cosmos Document Id needs to be recorded in the article document. 
    -	Only documents of type article are passed along to the queue for further processing as images and videos are processed in later steps.
2.	The Translate function is triggered by a queue event containing the article document id and utilizes the Translate Text Azure Cognitive Service. 
    -	Detect the language of the existing title and body of the article content, and determine whether:
        -	The language can be translated
        -	The language is different than the pre-defined translation language
    -	When necessary, translate the title and body text of the article.
    -	Where present, collect the sentiment, key phrases, and entities from both the body and the title.
    -	Write a record to the Processed collection in CosmosDB recording translation and analysis results, as well as how long the process took.
    -	Pass the article id on to the next queue. While this is inefficient if the article does not contain images or videos, it ensures this article, if there is anything interesting found, will be passed along at the end of the pipeline.
3.	The Detect function is triggered by a queue event containing the article document id and utilizes the Vision Azure Cognitive Services. If images are present on the article, the following steps are taken:
    -	Detect objects and landmarks in the image using the Detect API.
    -	Detect written words in the image using the OCR API.
    -	Write to, or create, a record in the Processed collection in CosmosDB recording the detection results by putting each detected object or text into the tags property on the processed record.
    -	Pass the article id to the next queue for processing.
4.	The Face function is triggered by a queue event containing the article document id and utilizes the Face Cognitive Service. If images are present on the article, the following steps are taken:
    -	Detect faces in the image and infer their gender and age using the Face API.
    -	Write to, or create, a record in the Processed collection in CosmosDB recording the detection results by putting each detected face (gender/age) into the tags property on the processed record.
    -	Pass the article id to the next queue for processing.
5.	The Notify function is triggered by a queue event containing the article document id.
    -	Load the processed records for the article and images.
    -	Scan the processed documents for “interesting” content.
    -	If an article or ANY of its associated images/videos trigger an “interesting”* flag, send a notification off to the system of choice.**

\* Interesting is something that must be defined by the end user of the architecture.

\** The final destination for notification on “interesting” articles is left to the reader to determine. 


#### Repository Content
So what is contained in this repository that will help you understand how to do this? Well, here is an explanation of the content of this repository and then there will be instructions on how to go through deploying this demo to your Azure Subscription.

Directory | Contents
--- | --- 
Deployment | Contains Azure ARM scripts, Windows PowerShell scripts, and directories containing applications or Azure Function code.
Deployment/Functions | Contains a single .ZIP file that contains the Azure Function code that will be deployed to an Azure Web Application during deployment. 
Deployment/RssGenerator | A C# based application that is used to read RSS feeds and submit them to a CosmosDB Collection.
RssGenerator | The source code for the C# based application that is used to read RSS feeds and submit them to a CosmosDB Collection
wwwroot | Contains the code and structure of the source files that make up the Azure Functions.


#### Pre-requisites to deploying this demo
This demo was written to explicitly execute the automation script on a Windows based computer. If preferred, a user could also follow an analogous method to create the individual Azure services manually. You will need:

- A Windows based computer (local or cloud).
- An Azure Subscription (paid). While you can create a free tier of any of the Cognitive Services, you can only create one free Cognitive Service in an account; the free tier would therefore not be able to handle all of the services in this example.
- An up-to-date version of PowerShell: https://docs.microsoft.com/en-us/powershell/azure/install-azurerm-ps?view=azurermps-6.13.0
- This repository, cloned to the <b>hard disk</b> of the Windows based computer.

#### Deploying the solution
1. Open up Windows PowerShell and navigate to the \Deployment subfolder in the directory where you cloned this repository. 

2. Open the file <b>Deployment.ps1</b> and at the top of the file enter in your Azure subscription ID, a name for the Azure Resource Group to hold all of the resources, and a geo location that will host the services. Not all regions are supported: if you're unsure what to use, we suggest that you choose `eastus` or `westus`. 

3. Run the script <b>Deployment.ps1</b> in that folder. You will be asked to log in to your Azure Subscription shortly after the script starts. After that, the process will not need your intervention until the all of the resources have been deployed. 

4. Once the run has completed, all of the required Azure resources are deployed to your subscription in the designated resource group. Further, the generator application will have been configured in \Deployment\RssGenerator so simply run \Deployment\RssGenerator\RssGenerator.exe to send some documents into the CosmosDB Document Collection.

5. Open the [Azure Portal](https://portal.azure.com), navigate to the resource group you indentified in the Deployment.ps1 file, clikc on the <b>Azure Cosmos DB Account</b> resource, and from there navigate into the Data Explorer. You will find a single database with 4 Collections. To see how the incoming articles are updated during processing, begin by viewing some examples in the Ingest collection, then move on to the Processed collection and finally the Inspection collection.  
  
#### Deleting the solution
It is recommended that once the solution has been reviewed that it be deleted from your subscription so that the subscription owner is not incurring ongoing costs of having the services deployed.

Simply navigate to the [Azure Portal](https://portal.azure.com) and navigate to the resource group you identified in the Deployment.ps1 file. On the resource group blade, click Delete and follow the instructions. Once you have deleted the resource group all of the associated resources will be deleted and your subscription will not longer be billed for the solution. 

# Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.microsoft.com.

When you submit a pull request, a CLA-bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., label, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
