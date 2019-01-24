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
using RssGenerator.CosmosDBHelper;
using RssGenerator.RSS;
using RssGenerator.StorageHelper;

namespace RssGenerator
{
    partial class Program
    {
        /// <summary>
        /// Entry point to the application.
        /// </summary>
        /// <param name="args">
        ///     None - Uploads the articles to Cosmos
        ///     seed - Creates the database and collections (not needed)
        ///     query - Performs a query on processed images 
        /// </param>
        static void Main(string[] args)
        {
            List<String> arguments = new List<string>(args);

            /////////////////////////////////////////////////////////////////////////////////////////////////////
            // Load the configuration settings that contain the CosmosDB, Azure Storage, and RSS feed information
            /////////////////////////////////////////////////////////////////////////////////////////////////////
            Configuration config = Configuration.GetConfiguration();

            /////////////////////////////////////////////////////////////////////////////////////////////////////
            // Creat the Azure Storage Utility
            /////////////////////////////////////////////////////////////////////////////////////////////////////
            AzureStorageUtility storageUtility = new AzureStorageUtility(config.StorageConnectionString);


            /////////////////////////////////////////////////////////////////////////////////////////////////////
            // Create the CosmosDB Client
            /////////////////////////////////////////////////////////////////////////////////////////////////////
            String returnResult = "Jobs completed";
            bool bWaitForUser = true;
            using (CosmosDbClient client = new CosmosDbClient(config.CosmosUri, config.CosmosKey))
            {
                if(arguments.Contains("seed") )
                {
                    bWaitForUser = false;
                    returnResult = Program.SeedDatabase(config, client);
                }
                else if(arguments.Contains("query"))
                {
                    returnResult = Program.QueryProcessedRecords(config, client);
                }
                else
                {
                    returnResult = Program.UploadRssFeeds(config, client, storageUtility);
                }
            }

            /////////////////////////////////////////////////////////////////////////////////////////////////////
            // Dispose of the static hash algorithm
            /////////////////////////////////////////////////////////////////////////////////////////////////////
            if (HashGenerator.HashAlgorithm != null)
            {
                HashGenerator.HashAlgorithm.Dispose();
            }

            Console.WriteLine(returnResult);
            if (bWaitForUser)
            {
                Console.WriteLine("Press any key to exit.");
                Console.ReadLine();
            }
        }
    }
}
