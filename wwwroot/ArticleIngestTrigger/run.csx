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

#r "Microsoft.Azure.DocumentDB.Core"
#load "../ArticleUtilties/All.csx"
using Newtonsoft.Json;
using Microsoft.Azure.Documents;
using System.Collections.Generic; 
using System;

// Function 1 of 4
// Function triggered by a listener on a CosmosDB collection (Article). It processes the record ID's and 
// passes them along to the TranslationQueueTrigger function through a queue notification.
public static async Task Run(IReadOnlyList<Document> input, ILogger log, ICollector<string> outputSbQueue)
{
    log.LogInformation("Document count " + input.Count);

    using(DocumentHelperUtility docUtility = new DocumentHelperUtility())
    {
        for(int i=0; i<input.Count; i++)
        {
            log.LogInformation("Working doc " + input[i].Id);

            docUtility.LoadRecords(input[i].Id, log); 
            if(docUtility.RawArticle != null) 
            {
                if(String.Compare(docUtility.RawArticle.ArtifactType, "article") != 0 )
                {
                    log.LogInformation("Record " + input[i].Id + " ignored because type is " + docUtility.RawArticle.ArtifactType);
                }
                else
                {
                    outputSbQueue.Add(input[i].Id);
                }
            }
            else
            {
                log.LogInformation("Record " + input[i].Id + " recieved but not found.");
            }
        }
    }

    log.LogInformation("Completed processing");
}