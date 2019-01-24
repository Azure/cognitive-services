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

using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Threading.Tasks;

namespace RssGenerator.StorageHelper
{
    /// <summary>
    /// Class that wraps teh Azure Storage functionality.
    /// </summary>
    class AzureStorageUtility
    {
        #region Private Members
        private CloudStorageAccount StorageAccount { get; set; }
        private CloudBlobClient BlobClient { get; set; }
        #endregion

        #region Public Members
        public String ConnectionString { get; private set;}
        #endregion

        public AzureStorageUtility(String connection)
        {
            this.ConnectionString = connection;

            this.StorageAccount = CloudStorageAccount.Parse(this.ConnectionString);
            this.BlobClient = this.StorageAccount.CreateCloudBlobClient();
        }

        /// <summary>
        /// Uploads a local file to blob storage into the specified container.
        /// </summary>
        /// <param name="path">Local file path</param>
        /// <param name="container">Azure Storage container name.</param>
        /// <returns>URI of the uploaded blob</returns>
        public async Task<String> UploadBlob(String path, String container)
        {
            String returnValue = String.Empty;

            CloudBlobContainer blobContainer = this.GetContainer(container);
            CloudBlockBlob cloudBlockBlob = blobContainer.GetBlockBlobReference(System.IO.Path.GetFileName(path));
            await cloudBlockBlob.UploadFromFileAsync(path);

            return cloudBlockBlob.Uri.AbsoluteUri;
        }

        /// <summary>
        /// Gets an instance of CloubBlobContainer. If the container doesn't exist
        /// it is created.
        /// </summary>
        /// <param name="container">Container name</param>
        /// <returns>Instance of CloudBlobContainer</returns>
        private CloudBlobContainer GetContainer(String container)
        {
            CloudBlobContainer returnContainer  = this.BlobClient.GetContainerReference(container);
            if (!returnContainer.Exists())
            {
                BlobContainerPermissions permissions = new BlobContainerPermissions
                {
                    PublicAccess = BlobContainerPublicAccessType.Blob
                };

                returnContainer.Create();
                returnContainer.SetPermissions(permissions);
            }

            return returnContainer;
        }
    }
}
