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
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.Azure.Documents.Client;

// Function 4 of 4
// Activated by an Azure Service Bus Queue.  It processes the record record for 
// OCR tasks. When complete, stamps the processed record to the 
// Processed collection.
// It is unclear what the next step here would be, but likely a final function 
// triggered off a final queue that would read the processed record and send 
// a notification (if needed) to the final destination.
public static void Run(string myQueueItem, ILogger log, out string outputSbQueue)
{
    log.LogInformation($"FaceAPIQueueTrigger function processed message: {myQueueItem}");

    using(DocumentHelperUtility docUtility = new DocumentHelperUtility())
    {
        docUtility.LoadRecords(myQueueItem, log);

        if(docUtility.RawArticle != null)
        {
            log.LogInformation("Found the raw article " + docUtility.RawArticle.UniqueIdentifier);

            // Do some work.....
            List<Dictionary<String, String>> imgs = docUtility.RawArticle.UnpackMedia(ArticleProperties.ChildImages);
            List<Dictionary<String, String>> vids = docUtility.RawArticle.UnpackMedia(ArticleProperties.ChildVideos);

            String faceUri = System.Environment.GetEnvironmentVariable("FaceAPIUri", EnvironmentVariableTarget.Process);
            String faceKey = System.Environment.GetEnvironmentVariable("FaceAPIKey", EnvironmentVariableTarget.Process);
            using(FaceService service = new FaceService(faceUri, faceKey))
            {
                foreach(Dictionary<String, String> image in imgs)
                {
                    log.LogInformation("Processing Image " + image[Article.MEDIA_INTERNAL_URI]);

                    // Create a processed record
                    Processed processedDoc = docUtility.LoadProcessedRecord(image[Article.MEDIA_ID],"image",log);

                    // Mark the start
                    DateTime start = DateTime.Now;
                    if(processedDoc.GetProperty(ProcessedProperties.ProcessedDateTime) == null)
                    {
                        processedDoc.SetProperty(ProcessedProperties.ProcessedDateTime, start.ToString("O"));
                    }

                    // Get the original tags so we don't overwrite them....
                    List<String> tags = processedDoc.UnpackTags();
                    FaceResults faceResults = new FaceResults();

                    log.LogInformation("Performing Face Detection");
                    FaceDetectionResult results = service.FaceIdentifyImage(image[Article.MEDIA_INTERNAL_URI]).Result;
                    if(results.Status == System.Net.HttpStatusCode.OK)
                    {
                        foreach(FaceId fid in results.Faces)
                        {
                            Person newPerson = new Person();
                            newPerson.Gender = fid.Gender;
                            newPerson.Age = fid.Age;
                            faceResults.People.Add(newPerson);
                        }
                    }
                    else
                    {
                        tags.Add("FACE DETECTION ERR: " + results.Status.ToString());
                    }
 
                    // Now write out the full group of tags
                    processedDoc.SetProperty(ProcessedProperties.Face, faceResults);
                    processedDoc.SetProperty(ProcessedProperties.Tags, tags);

                    // Mark the total execution time
                    if(processedDoc.GetProperty(ProcessedProperties.ProcessTime) == null)
                    {
                        processedDoc.SetProperty(ProcessedProperties.ProcessTime, (DateTime.Now - start).TotalMilliseconds.ToString());
                    }
                    else
                    {
                        double millis = (DateTime.Now - start).TotalMilliseconds;
                        double existing = double.Parse(processedDoc.GetProperty(ProcessedProperties.ProcessTime).ToString());
                        processedDoc.SetProperty(ProcessedProperties.ProcessTime, (millis + existing).ToString());
                    }

                    // Update the processed record
                    docUtility.InsertProcessedArticle(processedDoc, log).Wait(); 
                }
            }
        

            log.LogInformation("Finished Processing " + docUtility.RawArticle.UniqueIdentifier);
        }
    }

    // Pass along the ID to whatever might be listening....not implemented.
    outputSbQueue = myQueueItem;
}
