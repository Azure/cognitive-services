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
using Newtonsoft.Json;

namespace RssGenerator
{
    /// <summary>
    /// Sub item in the Configuration.json that identifies the 
    /// RSS feeds and storage container.
    /// </summary>
    class RssFeedInfo
    {
        [JsonProperty(PropertyName = "storage_container")]
        public String AzureStorageContainer { get; set; }
        [JsonProperty(PropertyName = "feed")]
        public String RSSFeed { get; set; }
    }

    /// <summary>
    /// Class that wraps the Configuration.json file that is used to drive
    /// the functionality. 
    /// </summary>
    class Configuration
    {
        [JsonIgnore]
        private const string FILE = "Configuration.json";

        /// <summary>
        /// URI of the CosmosDB
        /// </summary>
        [JsonProperty(PropertyName = "cosmos_db_uri")]
        public String CosmosUri { get; set; }
        
        /// <summary>
        /// Key to the CosmosDB
        /// </summary>
        [JsonProperty(PropertyName = "cosmos_db_key")]
        public String CosmosKey { get; set; }
        
        /// <summary>
        /// Cosmos Database To Use
        /// </summary>
        [JsonProperty(PropertyName = "cosmos_db_database")]
        public String CosmosDatabase { get; set; }
        
        /// <summary>
        /// Azure Storage Account connection string to put image attachments
        /// </summary>
        [JsonProperty(PropertyName = "azure_storage_connection")]
        public String StorageConnectionString { get; set; }
        
        /// <summary>
        /// CosmosDB Collection to use for ingest. This will be the one
        /// that has the ingest trigger associated with it.
        /// </summary>
        [JsonProperty(PropertyName = "cosmos_db_ingest_collection")]
        public String CosmosIngestCollection { get; set; }

        [JsonProperty(PropertyName = "cosmos_db_collections")]
        public List<String> CosmosCollectionList { get; set; }

        

        /// <summary>
        /// The RSS feeds to read and insert.
        /// </summary>
        [JsonProperty(PropertyName = "rss_feeds")]
        public List<RssFeedInfo> Feeds { get; set; }

        protected Configuration()
        {
            this.Feeds = new List<RssFeedInfo>();
            this.CosmosCollectionList = new List<string>();
        }

        public static Configuration GetConfiguration()
        {
            String path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Configuration.FILE);
            return JsonConvert.DeserializeObject<Configuration>(System.IO.File.ReadAllText(path));
        }
    }
}
