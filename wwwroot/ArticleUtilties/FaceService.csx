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

#r "Newtonsoft.Json"
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json;

class FaceId
{
    public String Id { get; set; }
    public String Gender { get; set; }
    public String Age { get; set; }

}

class FaceDetectionResult
{
    public System.Net.HttpStatusCode Status { get; set; }
    public List<FaceId> Faces { get; set; }

    public FaceDetectionResult()
    {
        this.Faces = new List<FaceId>();
    }
}

class FaceService : IDisposable
{
    #region Private Members
    private bool Disposed { get; set; }
    public const String FaceDetectionAttributes = "age,gender";
    public const String FaceDetectionUri = "/detect?returnFaceAttributes={0}";

    private System.Net.Http.HttpClient FaceApiClient { get; set; }
    #endregion

    public String FaceApiKey { get; private set; }
    public String BaseUri { get; private set; }

    public FaceService(string baseUri, string apiKey)
    {
        if (String.IsNullOrEmpty(baseUri))
        {
            throw new ArgumentNullException("baseUri");
        }
        if (String.IsNullOrEmpty(apiKey))
        {
            throw new ArgumentNullException("apiKey");
        }

        this.BaseUri = baseUri;
        this.FaceApiKey = apiKey;
    }

    public async Task<FaceDetectionResult> FaceIdentifyImage(String imageUri)
    {
        FaceDetectionResult returnResult = new FaceDetectionResult();

        if (this.FaceApiClient == null)
        {
            String parameters = String.Format(FaceService.FaceDetectionUri,
                FaceService.FaceDetectionAttributes);
            String uri = String.Format("{0}{1}", this.BaseUri, parameters);
            this.FaceApiClient = HttpClientHelper.CreateClient(uri, this.FaceApiKey);
        }

        String actualInput = "{\"url\" : \"" + imageUri + "\"}";

        HttpResponseMessage response = HttpClientHelper.MakeRequest(this.FaceApiClient,
                HttpClientHelper.RETRY_COUNT,
                HttpMethod.Post,
                actualInput,
                "application/json");

        returnResult.Status = response.StatusCode;
        if (response.StatusCode == System.Net.HttpStatusCode.OK)
        {
            string data = await response.Content.ReadAsStringAsync();

            var resultContent = JsonConvert.DeserializeObject(data);
            if (resultContent is Newtonsoft.Json.Linq.JArray)
            {
                foreach (var child in (resultContent as Newtonsoft.Json.Linq.JArray).Children())
                {
                    FaceId newId = new FaceId();

                    newId.Id = child.Value<String>("faceId");
                    Newtonsoft.Json.Linq.JObject att = child.Value<Newtonsoft.Json.Linq.JObject>("faceAttributes");

                    newId.Gender = att.Value<String>("gender");
                    newId.Age = att.Value<String>("age");

                    returnResult.Faces.Add(newId);
                }
            }
        }

        return returnResult;
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
        }

        this.Disposed = true;
    }
    #endregion
}