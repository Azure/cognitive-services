# wwwroot
<sup>Use Case: Mass Ingestion of Electronic Documents</sup> <br>
<sup>Created by Dan Grecoe, a Microsoft employee</sup>
 
 
 This directory contains the code for the Azure Functions that are required in the pipeline. 
 
 Content  | Description
---- | ----
host.json and extensions.csproj | These files are used by the Azure Function App to identify function version and required settings.
\bin | Contains the necessary binaries for the functions to execute.
\ArticleUtils | Common code shared amongst all of the functions.
\ArticleIngestTrigger | This function that is triggered when new documents are inserted to the Cosmos DB Collection.
\TranslationQueueTrigger | This function is triggered from the <i>translationqueue</i> Service Bus Queue that is populated from the ArticleIngestTrigger function. It processes the text of the article title and body.
\OcrQueueTrigger | This function is triggered from the <i>ocrqueue</i> Service Bus Queue that is populated from the TranslationQueueTrigger function. It processes the images reading out any text found in the image and identifying objects.
\FaceAPIQueueTrigger | This function is triggered from the <i>faceapiqueue</i> Service Bus Queue that is populated from the OcrQueueTrigger function. It processes the images finding faces and determining gender and age. 
\InspectionQueueTrigger | This function is triggered from the <i>translationqueue</i> Service Bus Queue that is populated from the FaceAPIQueueTrigger function. It processes the the results from the previous steps and does not forward any messages. 