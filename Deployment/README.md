# Deployment
<sup>Use Case: Mass Ingestion of Electronic Documents</sup> <br>
<sup>Created by Dan Grecoe, a Microsoft employee</sup>

This directory contains all of the scripts and code required to deploy and test this pipeline. 

Content  | Description
---- | ----
Deployment.ps1 | PowerShell script that orchetstrates the Azure service creation and seeding of application with the required settings to be used with this deployment.
\*.json | These files are Azure Resource Manager templates or ARM templates. They describe what Azure resources need to be created and how.
\Functions | Contains the zip file, which itself contains the Azure Function code. This file will be uploaded to a storage account and pointed to by the Azure Function App.
\RssGenerator | The binary version of the generator that will be used to seed the pipeline with articles.


## IMPORTANT
For the deployment to be succesful, you must first open Deployment.ps1 and modify the top of the file. Specifically you must at least provide your own subscription key and resource group name.

>$subscriptionId = "YOUR_SUBSCRIPTION_ID"<br>
>$resourceGroupName="YOUR_RESOURCE_GROUP_NAME"<br>
>$locationString = "eastus"

