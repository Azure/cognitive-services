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

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;

namespace RssGenerator.CosmosDBHelper
{
    class CosmosDbClient : IDisposable
    {
        #region Private Members
        private bool Disposed { get; set; }
        private Database CurrentDatabase { get; set; }
        private DocumentCollection CurrentCollection { get; set; }
        #endregion

        #region Public Members
        public String URI { get; private set; }
        public String KEY { get; private set; }
        public DocumentClient DocClient { get; set; }
        #endregion

        public CosmosDbClient(String uri, String key)
        {
            this.URI = uri;
            this.KEY = key;
        }

        /// <summary>
        /// Creates a document in a collection.
        /// </summary>
        /// <param name="database">Database containing the collection</param>
        /// <param name="collection">Collection</param>
        /// <param name="document">Document to insert</param>
        /// <returns>True if succesful</returns>
        public async Task<bool> CreateDocument(String database, String collection, object document)
        {
            this.CreateCollection(database, collection).Wait();

            Uri collectionUri = UriFactory.CreateDocumentCollectionUri(database, collection);

            var doc = await this.DocClient.CreateDocumentAsync(collectionUri, document);

            return (doc != null && doc.StatusCode == HttpStatusCode.Created);
        }

        /// <summary>
        /// Create the acutal DocumentClient to use on transactions.
        /// </summary>
        public void Connect()
        {
            if(String.IsNullOrEmpty(this.KEY))
            {
                throw new ArgumentNullException("KEY", "DB Key is missing");
            }
            if (String.IsNullOrEmpty(this.URI))
            {
                throw new ArgumentNullException("URI", "DB URI is missing");
            }

            if ( this.DocClient == null)
            {
                this.DocClient = new DocumentClient(new Uri(this.URI), this.KEY);
            }
        }

        /// <summary>
        /// Create a database and a collection within the database assuming they 
        /// don't already exist.
        /// </summary>
        /// <param name="database">Database name</param>
        /// <param name="collection">Collection name</param>
        public async Task CreateCollection(String database, String collection)
        {
            if (String.IsNullOrEmpty(database))
            {
                throw new ArgumentNullException("database", "database name is missing");
            }
            if (String.IsNullOrEmpty(this.URI))
            {
                throw new ArgumentNullException("collection", "collection name is missing");
            }

            try
            {
                this.Connect();
                Uri dbUri = UriFactory.CreateDatabaseUri(database);
                this.CurrentDatabase = await this.DocClient.CreateDatabaseIfNotExistsAsync(new Database { Id = database });

                IndexingPolicy indexing = new IndexingPolicy
                {
                    IndexingMode = IndexingMode.Consistent,
                    Automatic = true,
                    IncludedPaths = new System.Collections.ObjectModel.Collection<IncludedPath>
                    {
                        new IncludedPath
                        {
                            Path = "/*",
                            Indexes = new System.Collections.ObjectModel.Collection<Index>
                            {
                                new RangeIndex(DataType.Number, -1),
                                new RangeIndex(DataType.String,-1)
                            }
                        }
                    }
                };

                PartitionKeyDefinition partitionDef = new PartitionKeyDefinition
                {
                    Paths = new System.Collections.ObjectModel.Collection<string> { "/artifact_type" }
                };

                DocumentCollection docCollection = new DocumentCollection
                {
                    Id = collection,
                    PartitionKey = partitionDef,
                    IndexingPolicy = indexing
                };

                RequestOptions  options = new RequestOptions
                {
                    OfferThroughput = 10000
                };

                this.CurrentCollection = await this.DocClient.CreateDocumentCollectionIfNotExistsAsync(dbUri, docCollection, options);
            }
            catch(Exception ex)
            {
                String message = ex.Message;
            }
        }

        /*
        public async Task<int> DeleteDocuments(DocumentClient client, String database, String collection)
        {
            int count = 0;
            FeedOptions queryOptions = new FeedOptions {
                EnableCrossPartitionQuery = true
            };
            var sqlquery = "SELECT * FROM c";
            var articleQuery = client.CreateDocumentQuery<Document>(
                UriFactory.CreateDocumentCollectionUri(database, collection),
                sqlquery,
                queryOptions);

            foreach (var art in articleQuery)
            {
                ++count;
                await client.DeleteDocumentAsync(art.AltLink,
                    new RequestOptions() { PartitionKey = new PartitionKey("/artifact_type") });
            }
            return count;
        }
        */

        /// <summary>
        /// Collect a series of records from a single collection.
        /// </summary>
        /// <param name="client"></param>
        /// <param name="database"></param>
        /// <param name="collection"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public async Task<List<Newtonsoft.Json.Linq.JObject>> RetrieveRecords(DocumentClient client, String database, String collection, int max = 100)
        {
            List<Newtonsoft.Json.Linq.JObject> returnObjects = new List<Newtonsoft.Json.Linq.JObject>();

            using (IDocumentQuery<object> queryable = client.CreateDocumentQuery<object>(
                        UriFactory.CreateDocumentCollectionUri(database, collection),
                        new FeedOptions { EnableCrossPartitionQuery = true }).AsDocumentQuery())
            {
                while (queryable.HasMoreResults)
                {
                    foreach (object b in await queryable.ExecuteNextAsync<object>())
                    {
                        returnObjects.Add(Newtonsoft.Json.JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>(b.ToString()));
                        if(returnObjects.Count >= max)
                        {
                            break;
                        }
                    }
                }
            }

             return returnObjects;
        }

        /// <summary>
        /// Perform a query against a collection in Cosmos 
        /// </summary>
        /// <param name="client">Client to communicate with CosmosDB</param>
        /// <param name="database">Database name</param>
        /// <param name="collection">Collection name</param>
        /// <param name="sqlQuery">SQL Query</param>
        /// <returns></returns>
        public async Task<List<Newtonsoft.Json.Linq.JObject>> PerformQuery(DocumentClient client, String database, String collection, string sqlQuery)
        {
            List<Newtonsoft.Json.Linq.JObject> returnObjects = new List<Newtonsoft.Json.Linq.JObject>();

            using (IDocumentQuery<object> queryable = client.CreateDocumentQuery<object>(
                        UriFactory.CreateDocumentCollectionUri(database, collection),
                        sqlQuery).AsDocumentQuery())
            {
                while (queryable.HasMoreResults)
                {
                    foreach (object b in await queryable.ExecuteNextAsync<object>())
                    {
                        returnObjects.Add(Newtonsoft.Json.JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>(b.ToString()));
                    }
                }
            }

            return returnObjects;
        }

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
                if(this.DocClient != null )
                {
                    this.DocClient.Dispose();
                }
            }

            this.Disposed = true;
        }
        #endregion

    }
}
