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
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json;

class DetectionResult 
{
    public System.Net.HttpStatusCode Status { get; set; }
    public String Language { get; set; }
    public bool TranslationSupported { get; set; } 
}

class TranslationResult
{
    public System.Net.HttpStatusCode Status { get; set; }
    public String Translation { get; set; }
}

class TranslationService : IDisposable
{
    #region Private Members
    private bool Disposed { get; set; }
    private String PastLanguage { get; set; }

    // Example https://api.cognitive.microsofttranslator.com/detect?api-version=3.0
    private const String DetectionUri = "/detect?api-version=3.0";
    private const String TranslateUri = "/translate?api-version=3.0&from={0}&to={1}";
    private System.Net.Http.HttpClient TranslationDetectionClient { get; set; }
    private System.Net.Http.HttpClient TranslationClient { get; set; }
    #endregion

    public String TranslationApiKey { get; private set; }
    public String BaseUri { get; private set; }
    public String TargetLanguage { get; private set; }


    public TranslationService(string baseUri, string apiKey, string targetLanguage)
    {
        if(String.IsNullOrEmpty(baseUri))
        {
            throw new ArgumentNullException("baseUri");
        }
        if (String.IsNullOrEmpty(apiKey))
        {
            throw new ArgumentNullException("apiKey");
        }
        if (String.IsNullOrEmpty(targetLanguage))
        {
            throw new ArgumentNullException("targetLanguage");
        }

        this.BaseUri = baseUri;
        this.TranslationApiKey = apiKey;
        this.TargetLanguage = targetLanguage;
    }

    public async Task<DetectionResult> DetectLanguage(String content)
    {
        if (String.IsNullOrEmpty(content))
        {
            throw new ArgumentNullException("content");
        }

        DetectionResult returnResult = new DetectionResult();

        // Load the client
        this.LoadTranslationDetectionClient();

        // Prepare the input
        String actualInput = "[{\"Text\" : \"" + content + "\"}]";

        // Make the request
        HttpResponseMessage response = HttpClientHelper.MakeRequest(this.TranslationDetectionClient,
            HttpClientHelper.RETRY_COUNT,
            HttpMethod.Post,
            actualInput,
            "application/json");


        // Capture the status code, and if OK, get the rest of the data
        returnResult.Status = response.StatusCode;
        if (response.StatusCode == System.Net.HttpStatusCode.OK)
        {
            string data = await response.Content.ReadAsStringAsync();

            if (!String.IsNullOrEmpty(data))
            {
                Newtonsoft.Json.Linq.JArray jsonObj = JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JArray>(data);
                returnResult.Language = jsonObj.First.Value<String>("language");
                returnResult.TranslationSupported = jsonObj.First.Value<bool>("isTranslationSupported");
            }
        }

        return returnResult;
    }

    public async Task<TranslationResult> TranslateContent(DetectionResult detected, String content)
    {
        TranslationResult returnResult = new TranslationResult();

        if (detected == null)
        {
            throw new ArgumentNullException("detected");
        }
        if (String.IsNullOrEmpty(content))
        {
            throw new ArgumentNullException("content");
        }

        // Load the client
        this.LoadTranslationClient(detected.Language);

        String actualInput = "[{\"Text\" : \"" + content + "\"}]";

        // Make the request
        HttpResponseMessage response = HttpClientHelper.MakeRequest(
            this.TranslationClient,
            HttpClientHelper.RETRY_COUNT,
            HttpMethod.Post,
            actualInput,
            "application/json");

        // Capture the status code, and if OK, get the rest of the data
        returnResult.Status = response.StatusCode;

        if (response.StatusCode == System.Net.HttpStatusCode.OK)
        {
            string data = await response.Content.ReadAsStringAsync();

            if (!String.IsNullOrEmpty(data))
            {
                Newtonsoft.Json.Linq.JArray jsonObj = JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JArray>(data);
                Newtonsoft.Json.Linq.JArray translationsObj = jsonObj.First.Value<Newtonsoft.Json.Linq.JArray>("translations");
                returnResult.Translation = translationsObj.First.Value<String>("text");
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
            if(this.TranslationDetectionClient != null)
            {
                this.TranslationDetectionClient.Dispose();
            }
            if(this.TranslationClient != null)
            {
                this.TranslationClient.Dispose();
            }
        }

        this.Disposed = true;
    }
    #endregion

    #region Private Client Helpers
    private void LoadTranslationClient(String from)
    {
        if (String.IsNullOrEmpty(this.TranslationApiKey))
        {
            throw new Exception("Translation API Key Not Provided");
        }
        if (String.IsNullOrEmpty(from))
        {
            throw new ArgumentNullException("from");
        }

        if(this.TranslationClient == null || 
            String.IsNullOrEmpty(this.PastLanguage) ||
            String.Compare(this.PastLanguage, from) != 0)
        {
            String uriParameters = String.Format(TranslationService.TranslateUri, from, this.TargetLanguage);
            String uri = String.Format("{0}{1}", this.BaseUri, uriParameters);
            this.TranslationClient = HttpClientHelper.CreateClient(uri, this.TranslationApiKey);
        }

        this.PastLanguage = from;
    }

    private void LoadTranslationDetectionClient()
    {
        if (this.TranslationDetectionClient == null)
        {
            if (String.IsNullOrEmpty(this.TranslationApiKey))
            {
                throw new Exception("Translation API Key Not Provided");
            }

            String uri = String.Format("{0}{1}", this.BaseUri, TranslationService.DetectionUri);
            this.TranslationDetectionClient = HttpClientHelper.CreateClient(uri, this.TranslationApiKey);
        }
    }
    #endregion
}