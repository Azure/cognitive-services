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
using RssGenerator.CosmosDBHelper;
using System;
using System.Collections.Generic;

namespace RssGenerator
{
    partial class Program
    {

        private const String ARTICLE_QUERY_WITH_MEDIA_ID = @"SELECT * FROM c 
            WHERE c.artifact_type='article'
            AND ARRAY_CONTAINS(c.properties.child_images, {{ mediaId: '{0}'}},true)";

        /// <summary>
        /// Look into the Processed collection and find all images. Get the original image ID
        /// and then query the Ingest table to determine which original article it belonged to.
        /// 
        /// This function is an example on how to retrieve records by collection or by using
        /// a query string.
        /// </summary>
        /// <param name="config">Configuration with CosmosDB information</param>
        /// <param name="client">CLient to do work</param>
        /// <returns>String to present to user.</returns>
        public static String QueryProcessedRecords(Configuration config, CosmosDbClient client)
        {
            String returnValue = "Failed to query CosmosDB";

            client.Connect();

            List<Newtonsoft.Json.Linq.JObject> objects = client.RetrieveRecords(client.DocClient, config.CosmosDatabase, "Processed").Result;

            List<String> imageIds = new List<string>();
            foreach(Newtonsoft.Json.Linq.JObject jobj in objects)
            {
                String at = jobj.Value<string>("artifact_type");
                if (String.Compare(at, "image", true) == 0)
                {
                    imageIds.Add(jobj.Value<string>("parent"));
                }
            }

            if (imageIds.Count > 0)
            {
                returnValue = String.Format("Processed Images paired with parent Articles{0}{0}", Environment.NewLine);

                foreach (String imageId in imageIds)
                {
                    returnValue += String.Format("Image ID: {0}{1}Parent ID:{1}", imageId, Environment.NewLine);

                    String sqlQuery = String.Format(ARTICLE_QUERY_WITH_MEDIA_ID, imageId);
                    List<Newtonsoft.Json.Linq.JObject> parentArticles = client.PerformQuery(client.DocClient, config.CosmosDatabase, "Ingest", sqlQuery).Result;

                    foreach(Newtonsoft.Json.Linq.JObject jobj in parentArticles)
                    {
                        returnValue += String.Format("\t{0}{1}", jobj.Value<string>("id"), Environment.NewLine);
                    }
                }
            }

            return returnValue;
        }
    }
}
