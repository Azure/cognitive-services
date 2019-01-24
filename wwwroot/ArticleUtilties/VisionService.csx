

#r "Newtonsoft.Json"
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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

using System.Threading.Tasks;
using System.Net.Http;

class ImageAnalyzeResult
{
    public System.Net.HttpStatusCode Status { get; set; }
    public List<String> Categories { get; set; }
    public List<String> Tags { get; set; }

    public ImageAnalyzeResult()
    {
        this.Categories = new List<string>();
        this.Tags = new List<string>();
    }
}
class OcrResult
{
    public System.Net.HttpStatusCode Status { get; set; }
    public List<String> Lines { get; set; }

    public OcrResult()
    {
        this.Lines = new List<string>();
    }
}

class VisionService : IDisposable
{
    #region Private Members
    private bool Disposed { get; set; }
    private const String VisionAnalyzeFeatures = "Categories,Description,Tags,Adult";
    private const String VisionAnalyzeDetails = "Landmarks";
    private  const double VisionAnalyzeMinConfidence = 0.75;

    private const String VisionAnalyzeUri = "/vision/v2.0/analyze?visualFeatures={0}&language={1}&details={2}";
    private const String VisionOcrUri = "/vision/v2.0/ocr?language=unk&detectOrientation=true";

    private System.Net.Http.HttpClient AnalyzeClient { get; set; }
    private System.Net.Http.HttpClient OCRClient { get; set; }
    #endregion

    public String VisionApiKey { get; private set; }
    public String BaseUri { get; private set; }
    public String TargetLanguage { get; set; }

    public VisionService(String baseUri, String apiKey, String analyzeLang = "en")
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
        this.VisionApiKey = apiKey;
        this.TargetLanguage = analyzeLang;
    }

    public async Task<ImageAnalyzeResult> AnalyzeImage(String imageUri)
    {
        ImageAnalyzeResult results = new ImageAnalyzeResult();

        this.CreateAnalyzeClient();

        String actualInput = "{\"url\" : \"" + imageUri + "\"}";

        HttpResponseMessage response = HttpClientHelper.MakeRequest(this.AnalyzeClient,
            HttpClientHelper.RETRY_COUNT,
            HttpMethod.Post,
            actualInput,
            "application/json");

        results.Status = response.StatusCode;
        if (response.StatusCode == System.Net.HttpStatusCode.OK)
        {
            string data = await response.Content.ReadAsStringAsync();

            var resultContent = JsonConvert.DeserializeObject(data);
            if (resultContent is Newtonsoft.Json.Linq.JObject)
            {
                foreach (var child in (resultContent as Newtonsoft.Json.Linq.JObject).Children())
                {
                    if (child is Newtonsoft.Json.Linq.JProperty)
                    {
                        Newtonsoft.Json.Linq.JArray jArray = null;

                        if ((jArray = GetJArrayFromJProperty("categories", child as Newtonsoft.Json.Linq.JProperty)) != null)
                        {
                            foreach (var cat in jArray.Children())
                            {
                                String name = cat["name"].ToString();
                                String score = cat["score"].ToString();

                                if (double.Parse(score) >= VisionService.VisionAnalyzeMinConfidence)
                                {
                                    results.Categories.Add(name);
                                }
                            }
                        }

                        if ((jArray = GetJArrayFromJProperty("tags", child as Newtonsoft.Json.Linq.JProperty)) != null)
                        {
                            foreach (var tag in jArray.Children())
                            {
                                String name = tag["name"].ToString();
                                String conf = tag["confidence"].ToString();

                                if (double.Parse(conf) >= VisionService.VisionAnalyzeMinConfidence)
                                {
                                    results.Tags.Add(name);
                                }
                            }
                        }
                    }
                }
            }
        }

        return results;
    }

    public async Task<OcrResult> OcrImage(String imageUri)
    {
        OcrResult returnResult = new OcrResult();

        this.CreatOCRClient();

        String actualInput = "{\"url\" : \"" + imageUri + "\"}";

        HttpResponseMessage response = HttpClientHelper.MakeRequest(this.OCRClient,
                HttpClientHelper.RETRY_COUNT,
                HttpMethod.Post,
                actualInput,
                "application/json");

        returnResult.Status = response.StatusCode;
        if (response.StatusCode == System.Net.HttpStatusCode.OK)
        {
            // Create a JObject out of the content
            string data = await response.Content.ReadAsStringAsync();
            var resultContent = JsonConvert.DeserializeObject(data);

            Newtonsoft.Json.Linq.JArray regionArray = (resultContent as Newtonsoft.Json.Linq.JObject)["regions"] as Newtonsoft.Json.Linq.JArray;
            if (regionArray != null)
            {
                foreach (var child in regionArray.Children())
                {
                    if (child is Newtonsoft.Json.Linq.JObject)
                    {
                        Newtonsoft.Json.Linq.JArray lineArray = (child as Newtonsoft.Json.Linq.JObject)["lines"] as Newtonsoft.Json.Linq.JArray;

                        foreach (var line in lineArray.Children())
                        {
                            List<string> physicalLine = new List<string>();
                            Newtonsoft.Json.Linq.JArray wordsArray = (line as Newtonsoft.Json.Linq.JObject)["words"] as Newtonsoft.Json.Linq.JArray;
                            foreach (var word in wordsArray.Children())
                            {
                                physicalLine.Add(word["text"].ToString());
                            }

                            returnResult.Lines.Add(String.Join(" ", physicalLine));
                        }
                    }
                }
            }
        }

        return returnResult;
    }

    #region Private Helpers
    private void CreateAnalyzeClient()
    {
        if(this.AnalyzeClient == null)
        {
            String parameters = String.Format(VisionService.VisionAnalyzeUri,
                VisionService.VisionAnalyzeFeatures,
                this.TargetLanguage,
                VisionService.VisionAnalyzeDetails);
            String uri = String.Format("{0}{1}", this.BaseUri, parameters);

            this.AnalyzeClient = HttpClientHelper.CreateClient(uri, this.VisionApiKey);
        }
    }

    private void CreatOCRClient()
    {
        if(this.OCRClient == null)
        {
            String uri = String.Format("{0}{1}", this.BaseUri, VisionService.VisionOcrUri);
            this.OCRClient = HttpClientHelper.CreateClient(uri, this.VisionApiKey);
        }
    }


    private Newtonsoft.Json.Linq.JArray GetJArrayFromJProperty(String expectedName, Newtonsoft.Json.Linq.JProperty property)
    {
        Newtonsoft.Json.Linq.JArray returnArray = null;

        if (String.Compare(property.Name, expectedName, true) == 0)
        {
            var subArray = property.First;
            if (subArray != null && subArray is Newtonsoft.Json.Linq.JArray)
            {
                returnArray = subArray as Newtonsoft.Json.Linq.JArray;
            }
        }

        return returnArray;
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
            if(this.AnalyzeClient != null)
            {
                this.AnalyzeClient.Dispose();
            }
            if (this.OCRClient != null)
            {
                this.OCRClient.Dispose();
            }
        }

        this.Disposed = true;
    }
    #endregion
}