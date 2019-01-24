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
using System.Net.Http;

public class ContentEntity
{
    public String OriginalText { get; set; }
    public String Name { get; set; }
    public String BingId { get; set; }
    public String WikipediaUrl { get; set; }

}

public class EntityResponse
{
    public System.Net.HttpStatusCode Status { get; set; }
    public List<ContentEntity> Entities{ get; set; }
    public EntityResponse()
    {
        this.Entities = new List<ContentEntity>();
    }

} 

public class SentimentResponse
{
    public System.Net.HttpStatusCode Status { get; set; }
    public double Sentiment{ get; set; }
}

public class KeyPhraseResponse
{
    public System.Net.HttpStatusCode Status { get; set; }
    public List<String> Phrases { get; set; }

    public KeyPhraseResponse()
    {
        this.Phrases = new List<string>();
    }
}

class TextService : IDisposable
{
    #region Private Members
    private bool Disposed { get; set; }
    private System.Net.Http.HttpClient SentimentClient { get; set; }
    private System.Net.Http.HttpClient KeyPhraseClient { get; set; }
    private System.Net.Http.HttpClient EntitiesClient { get; set; }

    //https://westus.api.cognitive.microsoft.com/text/analytics/v2.0/sentiment

    // Values from 0-1, 0.5 is neutral 1 positive 0 negative
    private const String SentimentUri = "/sentiment";
    private const String KeyPhrasesUri = "/keyPhrases";
    private const String EntitiesUri = "/entities";
    private const String InputFormat = "{{\"documents\": [{{\"language\": \"{0}\",\"id\": \"1\",\"text\": \"{1}\"	}}]}}";
    #endregion

    public String TextApiKey { get; private set; }
    public String BaseUri { get; private set; }

    public TextService(string baseUri, string apiKey)
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
        this.TextApiKey = apiKey;
    }

    public async Task<KeyPhraseResponse> FindKeyPhrases(String content, String language)
    {
        KeyPhraseResponse returnPhrases = new KeyPhraseResponse();

        String query = this.GetQuery(content, language);
        this.CreateClient(TextService.KeyPhrasesUri);

        HttpResponseMessage response = HttpClientHelper.MakeRequest(this.KeyPhraseClient,
            HttpClientHelper.RETRY_COUNT,
            HttpMethod.Post,
            query,
            "application/json");

        returnPhrases.Status = response.StatusCode;
        if (response.StatusCode == System.Net.HttpStatusCode.OK)
        {
            string data = await response.Content.ReadAsStringAsync();

            Newtonsoft.Json.Linq.JObject json = Newtonsoft.Json.JsonConvert.DeserializeObject(data) as Newtonsoft.Json.Linq.JObject;
            Newtonsoft.Json.Linq.JArray docsArray = json["documents"] as Newtonsoft.Json.Linq.JArray;
            foreach(var value in docsArray)
            {
                Newtonsoft.Json.Linq.JArray keys = value["keyPhrases"] as Newtonsoft.Json.Linq.JArray;
                if(keys != null)
                {
                    foreach(var keyPhrase in keys)
                    {
                        returnPhrases.Phrases.Add(keyPhrase.ToString());
                    }
                }
            }
        }

        return returnPhrases;
    }

    public async Task<EntityResponse> FindEntities(String content, String language)
    {
        EntityResponse returnResponse = new EntityResponse();

        String query = this.GetQuery(content, language);
        this.CreateClient(TextService.EntitiesUri);

        HttpResponseMessage response = HttpClientHelper.MakeRequest(this.EntitiesClient,
            HttpClientHelper.RETRY_COUNT,
            HttpMethod.Post,
            query,
            "application/json");

        returnResponse.Status = response.StatusCode;
        if (response.StatusCode == System.Net.HttpStatusCode.OK)
        {
            string data = await response.Content.ReadAsStringAsync();

            Newtonsoft.Json.Linq.JObject json = Newtonsoft.Json.JsonConvert.DeserializeObject(data) as Newtonsoft.Json.Linq.JObject;
            Newtonsoft.Json.Linq.JArray docsArray = json["documents"] as Newtonsoft.Json.Linq.JArray;
            if (docsArray != null)
            {
                foreach (var value in docsArray)
                {
                    Newtonsoft.Json.Linq.JArray entitiesArray = (value as Newtonsoft.Json.Linq.JObject)["entities"] as Newtonsoft.Json.Linq.JArray;
                    if (entitiesArray != null)
                    {

                        foreach (var entity in entitiesArray)
                        {
                            List<String> matchesList = new List<string>();

                            ContentEntity newContentEntity = new ContentEntity();
                            newContentEntity.Name = entity.Value<String>("name");
                            newContentEntity.BingId = entity.Value<String>("bingId");
                            newContentEntity.WikipediaUrl = entity.Value<String>("wikipediaUrl");

                            Newtonsoft.Json.Linq.JArray matches = entity["matches"] as Newtonsoft.Json.Linq.JArray;
                            if (matches != null)
                            {
                                foreach (var match in matches)
                                {
                                    matchesList.Add(match.Value<String>("text"));
                                }
                            }

                            newContentEntity.OriginalText = String.Join(",", matchesList);
                            returnResponse.Entities.Add(newContentEntity);
                        }
                    }
                }
            }
        }
        return returnResponse;
    }
    public async Task<SentimentResponse> FindSentiment(String content, String language)
    {
        SentimentResponse returnResponse = new SentimentResponse();

        String query = this.GetQuery(content, language);
        this.CreateClient(TextService.SentimentUri);

        HttpResponseMessage response = HttpClientHelper.MakeRequest(this.SentimentClient,
            HttpClientHelper.RETRY_COUNT,
            HttpMethod.Post,
            query,
            "application/json");

        returnResponse.Status = response.StatusCode;
        if (response.StatusCode == System.Net.HttpStatusCode.OK)
        {
            string data = await response.Content.ReadAsStringAsync();

            Newtonsoft.Json.Linq.JObject json = Newtonsoft.Json.JsonConvert.DeserializeObject(data) as Newtonsoft.Json.Linq.JObject;
            Newtonsoft.Json.Linq.JArray docsArray = json["documents"] as Newtonsoft.Json.Linq.JArray;
            // We only have one so....still need to go through it...
            foreach (var value in docsArray)
            {
                returnResponse.Sentiment = double.Parse(value["score"].ToString());
            }
        }

        return returnResponse;
    }

    #region Client Generation
    private string GetQuery(String content, String language)
    {
        if(String.IsNullOrEmpty(content))
        {
            throw new ArgumentNullException("content");
        }
        if (String.IsNullOrEmpty(language))
        {
            throw new ArgumentNullException("language");
        }
        return String.Format(TextService.InputFormat, language, content);
    }

    private void CreateClient(String extension)
    {
        String uri = String.Format("{0}{1}", this.BaseUri, extension);
        System.Net.Http.HttpClient client = HttpClientHelper.CreateClient(uri, this.TextApiKey);

        switch (extension)
        {
            case TextService.SentimentUri:
                this.SentimentClient = client;
                break;
            case TextService.KeyPhrasesUri:
                this.KeyPhraseClient = client;
                break;
            case TextService.EntitiesUri:
                this.EntitiesClient = client;
                break;
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
            if(this.SentimentClient != null)
            {
                this.SentimentClient.Dispose();
            }
            if(this.KeyPhraseClient != null)
            {
                this.KeyPhraseClient.Dispose();
            }
            if (this.EntitiesClient!= null)
            {
                this.EntitiesClient.Dispose();
            }
        }

        this.Disposed = true;
    }
    #endregion

}