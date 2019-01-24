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


class DocumentHelperUtility : IDisposable
{
    #region Private Members
    private bool Disposed { get; set; }
    private String CosmosConnectionString { get; set; }
    private String CosmosDatabaseName { get; set; }
    private String CosmosRawCollectionName { get; set; }
    private String CosmosProcessedCollectionName { get; set; }
    private String CosmosInspectionCollectionName {get ;set;}
    private DocumentClient CosmosClient { get; set; }
    #endregion


    public Article RawArticle { get; private set; }
    //public Processed ProcessedRecord { get; private set; }

    public DocumentHelperUtility()
    {
        this.CosmosConnectionString = System.Environment.GetEnvironmentVariable("ArticleIngestTrigger_ConnectionString", EnvironmentVariableTarget.Process);
        this.CosmosDatabaseName = System.Environment.GetEnvironmentVariable("CosmosDbName", EnvironmentVariableTarget.Process);
        this.CosmosRawCollectionName = System.Environment.GetEnvironmentVariable("CosmosCollectionName", EnvironmentVariableTarget.Process);
        this.CosmosProcessedCollectionName = System.Environment.GetEnvironmentVariable("CosmosProcessedCollectionName", EnvironmentVariableTarget.Process);
        this.CosmosInspectionCollectionName = System.Environment.GetEnvironmentVariable("CosmosInspectionCollectionName", EnvironmentVariableTarget.Process);
        
        this.CosmosClient = CosmosInsertHelper.GetClientFromConnectionString(this.CosmosConnectionString);
    }

    #region Load original and processed records
    public void LoadRecords(String uniqueId, ILogger log)
    {
        this.RawArticle = CosmosInsertHelper.RetrieveArticle(this.CosmosClient, this.CosmosDatabaseName, this.CosmosRawCollectionName, uniqueId, log);
        //this.ProcessedRecord = CosmosInsertHelper.RetrieveProcessedRecord(this.CosmosClient, this.CosmosDatabaseName, this.CosmosProcessedCollectionName, uniqueId, log);
    }

    public Processed LoadProcessedRecord(string parentid, String type, ILogger log)
    {
        Processed returnRecord = null;

        if (!String.IsNullOrEmpty(parentid))
        {
            List<Processed> proc = this.GetProcessedRecords(parentid, log);
            if(proc.Count == 1)
            {
                returnRecord = proc[0];
            }
            else
            {
                returnRecord = new Processed
                {
                    Parent = parentid,
                    ArtifactType = type
                };
            }
        }


        return returnRecord;   
    }

    public List<Processed> GetProcessedRecords(ILogger log)
    {
        List<Processed> returnList = new List<Processed>();

        if(this.RawArticle != null)
        {
            returnList = CosmosInsertHelper.RetrieveProcessedRecords(this.CosmosClient,  this.CosmosDatabaseName, this.CosmosProcessedCollectionName, this.RawArticle.UniqueIdentifier , log);        
        }

        return returnList;
    }

    public List<Processed> GetProcessedRecords(String id, ILogger log)
    {
        List<Processed> returnList = new List<Processed>();

        if(this.RawArticle != null)
        {
            returnList = CosmosInsertHelper.RetrieveProcessedRecords(this.CosmosClient,  this.CosmosDatabaseName, this.CosmosProcessedCollectionName, id , log);        
        }

        return returnList;
    }
    
    #endregion

    #region Create Records
    public async Task InsertProcessedArticle(object processedDocument, ILogger log)
    {
        if(processedDocument != null)
        {
            bool insertOk = await CosmosInsertHelper.UpsertDocument(this.CosmosClient, this.CosmosDatabaseName, this.CosmosProcessedCollectionName, processedDocument);
            log.LogInformation("Upsert new processed doc: " + insertOk);
            
        }
        else
        {
            log.LogInformation("Processed document is null");
        }
    }

    public async Task CreateInspectionDocument(object inspectionDocument, ILogger log)
    {
        if(inspectionDocument != null)
        {
            bool insertOk = await CosmosInsertHelper.UpsertDocument(this.CosmosClient, this.CosmosDatabaseName, this.CosmosInspectionCollectionName, inspectionDocument);
            log.LogInformation("Upsert inspection doc   " + insertOk);
            
        }
        else
        {
            log.LogInformation("Inspection document is null");
        }
    }

    public async Task InsertRecord(object record, String database, String collection, ILogger log)
    {
        if(record != null)
        {
            log.LogInformation("Record to : " + database + " " + collection);
            bool insertOk = await CosmosInsertHelper.InsertRecord(this.CosmosClient, record, database, collection);
            log.LogInformation("Result of insert " + insertOk);
        }
        else
        {
            log.LogInformation("Record is null");
        }
    }
    #endregion

    #region IDisposable
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    // Protected implementation of Dispose pattern.
    protected virtual void Dispose(bool disposing)
    {
        if (this.Disposed)
            return;

        if (disposing)
        {
            if (this.CosmosClient != null )
            {
                this.CosmosClient.Dispose();
            }
        }

        this.Disposed = true;
    }
    #endregion
}