//
// Copyright  Microsoft Corporation ("Microsoft").
//
// Microsoft grants you the right to use this software in accordance with your subscription agreement, if any, to use software 
// provided for use with Microsoft Azure ("Subscription Agreement").  All software is licensed, not sold.  
// 
// If you do not have a Subscription Agreement, or at your option if you so choose, Microsoft grants you a nonexclusive, perpetual, 
// royalty-free right to use and modify this software solely for your internal business purposes in connection with Microsoft Azure 
// and other Microsoft products, including but not limited to, Microsoft R Open, Microsoft R Server, and Microsoft SQL Server.  
// 
// Unless otherwise stated in your Subscription Agreement, the following applies.  THIS SOFTWARE IS PROVIDED "AS IS" WITHOUT 
// WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL MICROSOFT OR ITS LICENSORS BE LIABLE 
// FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED 
// TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) 
// HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
// NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THE SAMPLE CODE, EVEN IF ADVISED OF THE
// POSSIBILITY OF SUCH DAMAGE.
//

#load "../ArticleUtilties/All.csx"
#r "Microsoft.Azure.DocumentDB.Core"
using System;
using System.Threading.Tasks;
using Microsoft.Azure.Documents.Client;

public static async Task  Run(string myQueueItem, ILogger log)
{
    log.LogInformation($"InspectionQueueTrigger function processed message: {myQueueItem}");

    using(DocumentHelperUtility docUtility = new DocumentHelperUtility())
    {
        docUtility.LoadRecords(myQueueItem, log); 

        if(docUtility.RawArticle != null)
        { 
            log.LogInformation("Found the raw article " + docUtility.RawArticle.UniqueIdentifier);

            // Get the images associated with the article
            List<Dictionary<String,String>> images = docUtility.RawArticle.UnpackMedia(ArticleProperties.ChildImages);

            // Get processed records associated with the article.
            List<Processed> processArticleResults = docUtility.GetProcessedRecords(log);
 
            // Get processed records associated with the images
            List<Processed> processImageResults = new List<Processed>();
            foreach(Dictionary<String,String> image in images)
            {
                processImageResults.AddRange(docUtility.GetProcessedRecords(image[Article.MEDIA_ID], log));
            }


            Dictionary<String,String> outputRecord = new Dictionary<String,String>();
            outputRecord.Add("artifact_type", "article");
            outputRecord.Add("Raw Article", docUtility.RawArticle.UniqueIdentifier);
            outputRecord.Add("Article Processed Steps", processArticleResults.Count.ToString());
            outputRecord.Add("Image Processed Steps", processImageResults.Count.ToString());

            // Get stats from title and body
            int keyPhraseCount = 0;
            int entityCount = 0;
            var bodyAnalyze = processArticleResults[0].GetProperty(ProcessedProperties.Body);
            if(bodyAnalyze != null && bodyAnalyze is TextFieldAnalytics)
            {
                keyPhraseCount = (bodyAnalyze as TextFieldAnalytics).Phrases.Count;
                entityCount = (bodyAnalyze as TextFieldAnalytics).Entities.Count;
            }

            var titleAnalyze = processArticleResults[0].GetProperty(ProcessedProperties.Title);
            if(titleAnalyze != null && titleAnalyze is TextFieldAnalytics)
            {
                keyPhraseCount += (titleAnalyze as TextFieldAnalytics).Phrases.Count;
                entityCount += (titleAnalyze as TextFieldAnalytics).Entities.Count;
            }
            outputRecord.Add("Key Phrase Count ", keyPhraseCount.ToString());
            outputRecord.Add("Entity Count ", entityCount.ToString());


            // Get information about images
            int categoryCount = 0;
            int objectCount = 0;
            int textLines = 0;
            int peopleCount = 0;
            foreach(Processed procImage in processImageResults)
            {
                var visionAnalyze = procImage.GetProperty(ProcessedProperties.Vision);
                if(visionAnalyze != null && visionAnalyze is VisionResults)
                {
                    categoryCount += (visionAnalyze as VisionResults).ObjectCategories.Count;
                    objectCount += (visionAnalyze as VisionResults).Objects.Count;
                    textLines += (visionAnalyze as VisionResults).Text.Count;
                }

                var faceAnalyze = procImage.GetProperty(ProcessedProperties.Face);
                if(faceAnalyze != null && faceAnalyze is FaceResults)
                {
                    categoryCount += (faceAnalyze as FaceResults).People.Count;
                }
            }
            outputRecord.Add("Image Category Count ", categoryCount.ToString());
            outputRecord.Add("Image Object Count ", objectCount.ToString());
            outputRecord.Add("Image Text Lines ", textLines.ToString());
            outputRecord.Add("Image Face Count ", peopleCount.ToString());

            // Now add everything to the article list
            processArticleResults.AddRange(processImageResults);
            
            float totalExecutionTime = 0;
            int tagCount = 0;
            foreach(Processed proc in processArticleResults)
            {
                totalExecutionTime += float.Parse(proc.GetProperty(ProcessedProperties.ProcessTime).ToString());

                List<String> tags = proc.UnpackTags();
                tagCount += tags.Count;
            }

            outputRecord.Add("Internal Processing Time ", totalExecutionTime.ToString());
            outputRecord.Add("Total Tags ", tagCount.ToString());
            
            docUtility.CreateInspectionDocument(outputRecord, log).Wait(); 

            log.LogInformation("INSP Finished Processing " + docUtility.RawArticle.UniqueIdentifier);
        }
    }
}