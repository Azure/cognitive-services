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
#load "AttributesExtension.csx"
using Newtonsoft.Json;

using System;

public enum ArticleProperties
{
    [PropertyDescriptor("original_uri")]
    OriginalUri,
    [PropertyDescriptor("internal_uri")]
    InternalUri,
    [PropertyDescriptor("retrieval_date")] 
    RetrievalDate,
    [PropertyDescriptor("post_date")]
    PostDate,
    [PropertyDescriptor("body")]
    Body,
    [PropertyDescriptor("title")]
    Title,
    [PropertyDescriptor("author")]
    Author,
    [PropertyDescriptor("hero_image")]
    HeroImage,
    [PropertyDescriptor("child_images")]
    ChildImages,
    [PropertyDescriptor("child_videos")]
    ChildVideos
};

public class Article
{
    [JsonIgnore]
    public const String MEDIA_ID = "mediaId";
    [JsonIgnore]
    public const String MEDIA_ORIG_URI = "origUri";
    [JsonIgnore]
    public const String MEDIA_INTERNAL_URI = "internalUri";
    
    [JsonProperty(PropertyName = "id")]
    public String UniqueIdentifier { get; set; }

    [JsonProperty(PropertyName = "asset_hash")]
    public String AssetHash { get; set; }

    [JsonProperty(PropertyName = "artifact_type")]
    public String ArtifactType { get; set; }

    [JsonProperty(PropertyName = "properties")]
    public Dictionary<String, object> Properties{ get; set; }

    public Article()
    {
        this.ArtifactType = "article";
        this.UniqueIdentifier = Guid.NewGuid().ToString("N");
        this.AssetHash = this.UniqueIdentifier;

        this.Properties = new Dictionary<string, object>();
    }

    public void SetProperty(ArticleProperties prop, object value)
    {
        String name = prop.JsonName();

        this.Properties[name] = value;
    }

    public object GetProperty(ArticleProperties prop)
    {
        String name = prop.JsonName();

        object returnValue = null;
        if(this.Properties.ContainsKey(name))
        {
            returnValue = this.Properties[name];
        }

        return returnValue;

    }

   public List<Dictionary<String,String>> UnpackMedia(ArticleProperties property)
   {
       if(property != ArticleProperties.ChildImages && property != ArticleProperties.ChildVideos)
       {
           throw new ArgumentException("Property type invalid.");
       }

       List<Dictionary<String, String>> returnPack = new List<Dictionary<string, string>>();

       var array = this.GetProperty(property);
       if (array != null && array is Newtonsoft.Json.Linq.JArray)
       {
           foreach (var child in (array as Newtonsoft.Json.Linq.JArray).Children())
           {
               String id = child[Article.MEDIA_ID].ToString();
               String orig = child[Article.MEDIA_ORIG_URI].ToString();
               String inter = child[Article.MEDIA_INTERNAL_URI].ToString();

               Dictionary<String, String> filePack = new Dictionary<string, string>();
               filePack.Add(Article.MEDIA_ID, id);
               filePack.Add(Article.MEDIA_ORIG_URI, orig);
               filePack.Add(Article.MEDIA_INTERNAL_URI, inter);
               returnPack.Add(filePack);
           }
       }

       return returnPack;
   }

    public override string ToString()
    {
        return JsonConvert.SerializeObject(this, Formatting.Indented);
    }
}