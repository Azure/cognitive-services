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
using RssGenerator.RecordFormats;
using RssGenerator.RSS;
using RssGenerator.StorageHelper;
using System;
using System.Collections.Generic;


namespace RssGenerator
{
    partial class Program
    {
        /// <summary>
        /// Upload the RSS feeds latest data that are identified in the configuration.
        /// </summary>
        /// <param name="config">Configuration information including RSS feeds</param>
        /// <param name="client">Cosmos DB client to upload to</param>
        /// <param name="storageUtility">Azure Storage client to store attachments in</param>
        /// <returns>Status string for output</returns>
        private static String UploadRssFeeds(Configuration config, CosmosDbClient client, AzureStorageUtility storageUtility)
        {
            /////////////////////////////////////////////////////////////////////////////////////////////////
            // Looop through each of the RSS feeds, collect the articles, then upload them.
            /////////////////////////////////////////////////////////////////////////////////////////////////
            foreach (RssFeedInfo feed in config.Feeds)
            {
                Console.WriteLine("Processing feed : " + feed.RSSFeed);
                using (RSSFeedReader rssReader = new RSSFeedReader(feed.RSSFeed))
                {
                    /////////////////////////////////////////////////////////////////////////////////////////
                    // The the batch of articles.....
                    /////////////////////////////////////////////////////////////////////////////////////////
                    int feedItemCount = 0;
                    List<RSSFeedItem> feedItems = rssReader.ReadFeed();
                    Console.WriteLine("Feed : " + feed.RSSFeed + " has " + feedItems.Count + " items");

                    /////////////////////////////////////////////////////////////////////////////////////////
                    // For each article, upload it's image(s) and content.
                    /////////////////////////////////////////////////////////////////////////////////////////
                    foreach (RSSFeedItem item in feedItems)
                    {
                        Console.WriteLine("Inserting : " + item.Title);

                        Article mainArticle = new Article();
                        List<Article> imageContent = new List<Article>();

                        // Set up the images
                        foreach (RSSImageItem image in item.Images)
                        {
                            String blobUri = storageUtility.UploadBlob(image.Path, feed.AzureStorageContainer).Result;
                            if (!String.IsNullOrEmpty(blobUri))
                            {
                                Article media = new Article();
                                media.ArtifactType = "image";
                                media.AssetHash = image.Hash;
                                media.UniqueIdentifier = image.Id;
                                media.SetProperty(ArticleProperties.RetrievalDate, DateTime.Now.ToString("O"));
                                media.SetProperty(ArticleProperties.OriginalUri, image.Uri);
                                media.SetProperty(ArticleProperties.InternalUri, blobUri);
                                imageContent.Add(media);
                            }
                        }

                        // Set up the article
                        mainArticle.SetProperty(ArticleProperties.OriginalUri, item.Uri);
                        mainArticle.SetProperty(ArticleProperties.RetrievalDate, DateTime.Now.ToString("O"));
                        mainArticle.SetProperty(ArticleProperties.PostDate, item.PublishedDate);
                        mainArticle.SetProperty(ArticleProperties.Title, Program.CleanInput(item.Title));
                        mainArticle.SetProperty(ArticleProperties.Body, Program.CleanInput(item.Summary));

                        // Create the child entries and attach to teh article.
                        List<Dictionary<string, string>> childFiles = new List<Dictionary<string, string>>();
                        foreach (Article file in imageContent)
                        {
                            Dictionary<String, String> obj = new Dictionary<string, string>();
                            obj.Add(Article.MEDIA_ID, file.UniqueIdentifier);
                            obj.Add(Article.MEDIA_ORIG_URI, file.GetProperty(ArticleProperties.OriginalUri).ToString());
                            obj.Add(Article.MEDIA_INTERNAL_URI, file.GetProperty(ArticleProperties.InternalUri).ToString());
                            childFiles.Add(obj);
                        }
                        mainArticle.SetProperty(ArticleProperties.ChildImages, childFiles);
                        mainArticle.SetProperty(ArticleProperties.ChildVideos, null);
                        mainArticle.SetProperty(ArticleProperties.Author, null);
                        mainArticle.SetProperty(ArticleProperties.HeroImage, null);

                        // Insert the media file documents to cosmos first
                        foreach (Article imageArticle in imageContent)
                        {
                            try
                            {
                                bool imageResult = client.CreateDocument(config.CosmosDatabase, config.CosmosIngestCollection, imageArticle).Result;
                                Console.WriteLine("Image Insert: " + imageResult.ToString());
                            }
                            catch (Exception ex) { }
                        }

                        // Wait briefly....
                        System.Threading.Thread.Sleep(500);

                        // Insert the article document to cosmos second
                        bool articleResult = client.CreateDocument(config.CosmosDatabase, config.CosmosIngestCollection, mainArticle).Result;
                        Console.WriteLine("Article Insert: " + articleResult.ToString());

                        // Ease of use to restrict the flow of records, uncomment the break statement. 
                        if (++feedItemCount > 5)
                        {
                            //break;
                        }
                    }
                }
            }

            return "Finished uploading current articles.";
        }

        /// <summary>
        /// Have to clean up the text that may be translated. HTML tags and double quotes 
        /// cause the Translation API to fail.This applies to title and body.
        /// </summary>
        private static String CleanInput(String input)
        {
            String returnValue = input;
            if (!String.IsNullOrEmpty(input))
            {
                int idxStart = 0;
                String tempValue = input;
                while ((idxStart = tempValue.IndexOf('<')) != -1)
                {
                    String resultString = String.Empty;
                    int idxEnd = tempValue.IndexOf('>');
                    if (idxStart > 0)
                    {
                        resultString = tempValue.Substring(0, idxStart);
                    }

                    resultString += tempValue.Substring(idxEnd + 1);
                    tempValue = resultString;
                }

                returnValue = tempValue;
            }

            // Finally strip out any double quotes...causes an exception in the translation API
            return returnValue.Replace('\"', '\'');
        }
    }
}
