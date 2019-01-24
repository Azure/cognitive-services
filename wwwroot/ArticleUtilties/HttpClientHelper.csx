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
using System.Net.Http;
using System.Net.Http.Headers;

class HttpClientHelper
{
    public const int RETRY_COUNT = 3;
    public const String CognitiveAPIKeyHeader = "Ocp-Apim-Subscription-Key";

    public static System.Net.Http.HttpClient CreateClient(string uri, string apiKey)
    {
        System.Net.Http.HttpClient client = new System.Net.Http.HttpClient();

        client.DefaultRequestHeaders.Add(HttpClientHelper.CognitiveAPIKeyHeader, apiKey);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        client.BaseAddress = new Uri(uri);

        return client;
    }

    public static System.Net.Http.HttpRequestMessage GetRequest(HttpMethod method, String content, String contentType)
    {
        var request = new HttpRequestMessage(method, string.Empty);
        request.Content = new StringContent(content);
        if (!String.IsNullOrEmpty(contentType))
        {
            request.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        }
        return request;
    }

    public static HttpResponseMessage MakeRequest(HttpClient client, int maxAttempts, HttpMethod method, String content, String contentType)
    {
        int attempts = 0;
        HttpResponseMessage returnMessage;

        // Create the request
        var request = HttpClientHelper.GetRequest(method, content, contentType);

        // Make request
        returnMessage = client.SendAsync(request).Result;
        while (!returnMessage.IsSuccessStatusCode && attempts < maxAttempts)
        {
            // Give it a few clicks to see if we are in trouble
            System.Threading.Thread.Sleep(10);

            // Can't send same request twice, so build another....
            request = HttpClientHelper.GetRequest(method, content, contentType);

            attempts++;
            returnMessage = client.SendAsync(request).Result;
        }

        return returnMessage;
    }
}