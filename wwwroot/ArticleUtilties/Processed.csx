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

using System;
using Newtonsoft.Json;

public enum ProcessedProperties
{
    [PropertyDescriptor("processed_datetime")]
    ProcessedDateTime,
    [PropertyDescriptor("processed_time")]
    ProcessTime,
    // Instances of TextFieldAnalytics
    [PropertyDescriptor("body")]
    Body,
    // Instances of TextFieldAnalytics
    [PropertyDescriptor("title")]
    Title, 
    // Instances of VisionResults
    [PropertyDescriptor("vision")]
    Vision,
    // Instances of FaceResults
    [PropertyDescriptor("face")]
    Face,
    [PropertyDescriptor("tags")]
    Tags
};

public class Person
{
    [JsonProperty(PropertyName = "gender")]
    public String Gender { get; set; }
    [JsonProperty(PropertyName = "age")]
    public String Age { get; set; }
}

public class FaceResults
{
    [JsonProperty(PropertyName = "people")]
    public List<Person> People { get; set; }
    public FaceResults()
    {
        this.People = new List<Person>();
    }
}

public class VisionResults
{
    [JsonProperty(PropertyName = "object_categories")]
    public List<String> ObjectCategories { get; set; }
    [JsonProperty(PropertyName = "objects")]
    public List<String> Objects { get; set; }
    [JsonProperty(PropertyName = "text")]
    public List<String> Text { get; set; }

    public VisionResults()
    {
        this.ObjectCategories = new List<string>();
        this.Objects = new List<string>();
        this.Text = new List<string>();
    }
}

public class TextFieldAnalytics
{
    [JsonProperty(PropertyName = "type")]
    public String FieldIdentifier { get; set; }
    [JsonProperty(PropertyName = "orig_lang_code")]
    public String OriginalLanguage { get; set; }
    [JsonProperty(PropertyName = "lang_code")]
    public String Language { get; set; }
    [JsonProperty(PropertyName = "value")]
    public String Value { get; set; }
    [JsonProperty(PropertyName = "key_phrases")]
    public List<String> Phrases { get; set; }
    [JsonProperty(PropertyName = "sentiment")]
    public double Sentiment { get; set; }
    [JsonProperty(PropertyName = "entities")]
    public List<ContentEntity> Entities { get; set; }

    public TextFieldAnalytics()
    {
        this.Phrases = new List<string>();
        this.Entities = new List<ContentEntity>();
    }
}


public class Processed
{
    [JsonProperty(PropertyName = "id")]
    public String UniqueIdentifier { get; set; }

    [JsonProperty(PropertyName = "parent")]
    public String Parent { get; set; }

    [JsonProperty(PropertyName = "asset_hash")]
    public String AssetHash { get; set; }

    [JsonProperty(PropertyName = "artifact_type")]
    public String ArtifactType { get; set; }

    [JsonProperty(PropertyName = "properties")]
    public Dictionary<String, object> Properties { get; set; }

    public Processed()
    {
        this.ArtifactType = "article";
        this.UniqueIdentifier = Guid.NewGuid().ToString("N");
        this.AssetHash = this.UniqueIdentifier;

        this.Properties = new Dictionary<string, object>();
    }

    public void SetProperty(ProcessedProperties prop, object value)
    {
        String name = prop.JsonName();

        this.Properties[name] = value;
    }

    public object GetProperty(ProcessedProperties prop)
    {
        String name = prop.JsonName();

        object returnValue = null;
        if (this.Properties.ContainsKey(name))
        {
            returnValue = this.Properties[name];
      
            switch(prop) {
                case ProcessedProperties.Title:
                case ProcessedProperties.Body:
                    if (returnValue != null)
                    {
                        returnValue = JsonConvert.DeserializeObject<TextFieldAnalytics>(JsonConvert.SerializeObject(returnValue));
                    }
                    break;
                case ProcessedProperties.Vision:
                    if (returnValue != null)
                    {
                        returnValue = JsonConvert.DeserializeObject<VisionResults>(JsonConvert.SerializeObject(returnValue));
                    }
                    break;
                case ProcessedProperties.Face:
                    if (returnValue != null)
                    {
                        returnValue = JsonConvert.DeserializeObject<FaceResults>(JsonConvert.SerializeObject(returnValue));
                    }
                    break;
            }
        }

        return returnValue;

    }

    public List<String> UnpackTags()
    {
        List<String> returnValue = new List<string>();

        var array = this.GetProperty(ProcessedProperties.Tags);
        if (array != null && array is Newtonsoft.Json.Linq.JArray)
        {
            foreach (var child in (array as Newtonsoft.Json.Linq.JArray).Children())
            {
                returnValue.Add(child.ToString());
            }
        }

        return returnValue;
    }

    public override string ToString()
    {
        return JsonConvert.SerializeObject(this, Formatting.Indented);
    }
}