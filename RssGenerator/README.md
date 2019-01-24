# RssGenerator
<sup>Use Case: Mass Ingestion of Electronic Documents</sup> <br>
<sup>Created by Dan Grecoe, a Microsoft employee</sup>

This directory contains the source code used to build the generator application that feeds the pipeline for this demo.

## Data Formats

### Ingestion format
This format is contains the original content of the article. Articles are broken down into article (contains text) and image entries in the Ingest collection in teh Cosmos DB. 

```json
{
	"id" : "GUID",
	"asset_hash" : "hash of the item",
	"artifact_type" : "article|image",
	"properties" :
		{
			Dependent on artifact_type
		}
}
```

#### Property Bag Properties
Property	| Type | Required |	Article |	Image 
----|-----|-----|-----|-----
original_uri |	String	|Y	|X	|X	
retrieval_datetime |	DateTime | Y	|X	|X	
post_date	|DateTime	|N	|X |		
body	|String	|N	|X |		
title	|String	|N	|X |		
author	|String	|N	|X |		
hero_image	|String	|N	|X |		
child_images	|Array(object)	|N	|X |		
internal_uri	|String	N		|X	|X |

##### Media Object
The media object is used for child_images. The field media_id is the Document ID of the media document in the Articles table. 
```json
{
    "mediaId": "9d30724f5b8043e49552f4b8eb02f010",
    "origUri": "https://dummy/thirdgrade.jpg",
    "internalUri": "https://dangtestrepo.blob.core.windows.net/scraped/thirdgrade.jpg"
}
```

### Processed Format
This format is contains the results of analyzing a portion of the ingested article. There will be one for the main article and one for each image. These records are kept in the Processed collection in Cosmos DB.

```json
{
	"id" : "GUID",
	"artifact_type" : "article|image", 
        “parent” : “parent id”,
	"properties" : {
			.... dependent on artifact type ......
	}
	"tags" :[interesting/need alerting/dealers choice!]
}
```

#### Property Bag Properties
Property	| Type | Required |	Article |	Image 
----|-----|-----|-----|-----
processed_datetime	|DateTime	|Y	|X	|X	
processed_time*	|Int	|Y	|X	|X	
title**	|object	|N	|X |		
body**	|object	|N	|X |		
vision***	|object	|N	| |X		
face****	|object	|N	| |X		
tags	|Array(string)	|N	|X	|X	
\* Total processing time (ms)

\** Text Field Analytics objects

\*** Vision Analytics object

\*** Face Analytics object

##### Text Field Analytics Object
```json
"body|title": {
    "type": "Body|Title",
    "orig_lang_code": "language detected",
    "lang_code": "requested language",
    "value": "Translated text content",
    "key_phrases": [
        "Array of strings, key phrases found"
    ],
    "sentiment": 0.5,
    "entities": [
        {
            "OriginalText": "(array of items found) British premier",
            "Name": "Prime Minister of the United Kingdom",
            "BingId": "2570ebea-8c42-048a-3350-57c9e4169167",
            "WikipediaUrl": "https://en.wikipedia.org/wiki/Prime_Minister_of_the_United...."
        }
		....
    ]
}
```

##### Vision Analytics Object
```json
"vision": {
     "object_categories": ["array of strings of object categories found"],
     "objects": ["array of strings of objects"],
     "text": ["array of strings of text found in images"]
 }
```

##### Face Analytics Object
The face object is a list of People with gender and age.
```json
"face": {
    "people": [
		{
			"gender" : "gender of person found",
			"age" : "age of person found"
		}
	]
}
```
