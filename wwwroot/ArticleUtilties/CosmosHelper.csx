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
#r "Newtonsoft.Json"
using Newtonsoft.Json;
using System;
using System.Net;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;

class CosmosInsertHelper
{
	public static DocumentClient GetClientFromConnectionString(String connectionString)
    {
		String[] parts = connectionString.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
        String uri = parts[0].Substring(parts[0].IndexOf('=')+1);
        String key = parts[1].Substring(parts[1].IndexOf('=')+1);

        return new DocumentClient(new Uri(uri), key);
        
    }

    #region Document Updates
    public async static Task<bool> InsertRecord(DocumentClient client, object record, String database, String collection)
    {
        bool returnValue = false;

        Uri collectionUri = UriFactory.CreateDocumentCollectionUri(database, collection);
        var doc = await client.CreateDocumentAsync(collectionUri, record);
        if(doc != null && doc.StatusCode == HttpStatusCode.Created)
        {
            returnValue = true;
        }

        return returnValue;
    }

    public static async Task<bool> UpsertDocument(DocumentClient client, String database, String collection, object document)
    {
        bool returnValue = true;
        var upDocument = await client.UpsertDocumentAsync(
                                UriFactory.CreateDocumentCollectionUri(database, collection),
                                document);
        
        if(upDocument == null)
        {
            returnValue = false;
        }
        return returnValue;
    }
    #endregion

    public static Article RetrieveArticle(DocumentClient client, String database, String collection, String id, ILogger log)
    {
        Article returnArticle = null;

        log.LogInformation("Find article in " + database + " " + collection);
        log.LogInformation("Article ID " + id);
        FeedOptions queryOptions = new FeedOptions {EnableCrossPartitionQuery = true};

        IQueryable<Article> articleQuery = client.CreateDocumentQuery<Article>(
            UriFactory.CreateDocumentCollectionUri(database, collection),
            queryOptions)
            .Where( f => f.UniqueIdentifier == id );
        
        if(articleQuery != null && articleQuery.Count() == 1)
        {
            foreach(Article art in articleQuery)
            {
                returnArticle = art;
                break;
            }
        }

        return returnArticle;
    }

    public static List<Processed> RetrieveProcessedRecords(DocumentClient client, String database, String collection, String parentid, ILogger log)
    {
        List<Processed> returnList = new List<Processed>();

        log.LogInformation("Find item in " + database + " " + collection);
        log.LogInformation("Parent ID " + parentid);
        FeedOptions queryOptions = new FeedOptions {EnableCrossPartitionQuery = true};

        IQueryable<Processed> processedQuery = client.CreateDocumentQuery<Processed>(
            UriFactory.CreateDocumentCollectionUri(database, collection),
            queryOptions)
            .Where( f => f.Parent == parentid );
        
        if(processedQuery != null)
        {
            foreach(Processed art in processedQuery)
            {
                returnList.Add(art);
            }
        }

        return returnList;
    }

}