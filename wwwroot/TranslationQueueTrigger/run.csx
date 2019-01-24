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
#load "TextProcessing.csx"
#r "Microsoft.Azure.DocumentDB.Core"
using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.Azure.Documents.Client;

// Function 2 of 4
// Function by an Azure Service Bus Queue.  It processes the record record for 
// text translation/detection. When complete, stamps the processed record to the 
// Processed collection and passes the ID to the next queue.
public static void Run(string myQueueItem, ILogger log, out string outputSbQueue)
{
    log.LogInformation($"TranslationQueueTrigger function processed message: {myQueueItem}");

    using(DocumentHelperUtility docUtility = new DocumentHelperUtility())
    {
        docUtility.LoadRecords(myQueueItem, log);

        if(docUtility.RawArticle != null)
        {
            log.LogInformation("Found the raw article " + docUtility.RawArticle.UniqueIdentifier);

            Processed processedDoc = docUtility.LoadProcessedRecord(docUtility.RawArticle.UniqueIdentifier, docUtility.RawArticle.ArtifactType,log);

            // Mark the start
            DateTime start = DateTime.Now;
            processedDoc.SetProperty(ProcessedProperties.ProcessedDateTime, start.ToString("O"));

            // Get the data.
            String title = docUtility.RawArticle.GetProperty(ArticleProperties.Title).ToString();
            String body = docUtility.RawArticle.GetProperty(ArticleProperties.Body).ToString();
            
            // Get the translation API key and create a service object
            String translationUri = System.Environment.GetEnvironmentVariable("TranslationAPIUri", EnvironmentVariableTarget.Process);
            String translationKey = System.Environment.GetEnvironmentVariable("TranslationAPIKey", EnvironmentVariableTarget.Process);
            String translationLanguage = System.Environment.GetEnvironmentVariable("TranslationAPITargetLanguage", EnvironmentVariableTarget.Process);

            //Get the text API stuff as well.
            String textUri = System.Environment.GetEnvironmentVariable("TextAPIUri", EnvironmentVariableTarget.Process);
            String textKey = System.Environment.GetEnvironmentVariable("TextAPIKey", EnvironmentVariableTarget.Process);
            
            using(TranslationService service = new TranslationService(translationUri, translationKey, translationLanguage))
            {
                using(TextService textService = new TextService(textUri, textKey))
                {
                    //////////////////////////////////////////////////////////////////////
                    // Detect/Translate/Analyze the title.
                    //////////////////////////////////////////////////////////////////////
                    TextFieldAnalytics titleAnalytics = null;
                    if(!String.IsNullOrEmpty(title))
                    {
                        titleAnalytics = AnalyzeText(service, textService, translationLanguage, ProcessedProperties.Title, title);
                    }
                    processedDoc.SetProperty(ProcessedProperties.Title, titleAnalytics);

                    //////////////////////////////////////////////////////////////////////
                    // Detect/Translate/Analyze the body second.
                    //////////////////////////////////////////////////////////////////////
                    TextFieldAnalytics bodyAnalytics = null;
                    if(!String.IsNullOrEmpty(body)) 
                    {
                        bodyAnalytics = AnalyzeText(service, textService, translationLanguage, ProcessedProperties.Body, body);
                    }
                    processedDoc.SetProperty(ProcessedProperties.Body, bodyAnalytics);
                }
            }

            // Mark the total execution time
            processedDoc.SetProperty(ProcessedProperties.ProcessTime, (DateTime.Now - start).TotalMilliseconds.ToString());

            // Tag it with something usefull....
            List<String> tags = new List<String>();
            processedDoc.SetProperty(ProcessedProperties.Tags, tags);

            // Update the processed record
            docUtility.InsertProcessedArticle(processedDoc, log).Wait(); 

            log.LogInformation("Finished Processing " + docUtility.RawArticle.UniqueIdentifier);
        }
    }

    // Send the id on to the next queue. Send null if you want to stop the chain.
    outputSbQueue = myQueueItem; 
}
