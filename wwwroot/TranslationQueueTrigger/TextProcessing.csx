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

#load "../ArticleUtilties/All.csx"
using System;
using System.Globalization;
using System.Threading.Tasks;



static TextFieldAnalytics AnalyzeText(TranslationService translationService, TextService textService, String translationLanguage, ProcessedProperties prop, String content)
{
    TextFieldAnalytics returnAnalytics = new TextFieldAnalytics();
    returnAnalytics.FieldIdentifier = prop.ToString();
    returnAnalytics.Language = translationLanguage;

    DetectionResult result = translationService.DetectLanguage(content).Result;
    bool processingError = false;
    if(result != null && result.Status == System.Net.HttpStatusCode.OK && result.TranslationSupported)
    {
        returnAnalytics.OriginalLanguage = result.Language;

        if(String.Compare(result.Language, translationLanguage) == 0 )
        {
            returnAnalytics.Value = content;
        }
        else
        {
            TranslationResult tresult = translationService.TranslateContent(result, content).Result;
            if(tresult != null && tresult.Status == System.Net.HttpStatusCode.OK)
            {
                returnAnalytics.Value = tresult.Translation;
            }
            else
            {
                processingError = true;
                returnAnalytics.Value = tresult.Status.ToString();
            }
        }
    }                
    else
    {
        processingError = true;
        returnAnalytics.Value =  "Detection Error : " + result.Status.ToString();
    }

    // Perform sentiment/entity/key phrase on translated content
    if(!processingError)
    {
        KeyPhraseResponse phrasesResponse = textService.FindKeyPhrases(returnAnalytics.Value, translationLanguage).Result;
        SentimentResponse sentimentResponse = textService.FindSentiment(returnAnalytics.Value, translationLanguage).Result;
        EntityResponse entityResponse = textService.FindEntities(returnAnalytics.Value, translationLanguage).Result;
        
        if(phrasesResponse != null && phrasesResponse.Status == System.Net.HttpStatusCode.OK)
        {
            returnAnalytics.Phrases =  phrasesResponse.Phrases;
        }
        if(sentimentResponse != null && sentimentResponse.Status == System.Net.HttpStatusCode.OK)
        {
            returnAnalytics.Sentiment =  sentimentResponse.Sentiment;
        }
        if(entityResponse != null && entityResponse.Status == System.Net.HttpStatusCode.OK)
        {
            returnAnalytics.Entities = entityResponse.Entities;
        }
    }

    return returnAnalytics;
}