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

#r "Microsoft.Azure.DocumentDB.Core"
#load "All.csx"
using System;
using System.Threading.Tasks;
using Microsoft.Azure.Documents.Client;

public static async void Run(object req)
{
    // Parameters for the services - Translator Text Service
    String translationUri = System.Environment.GetEnvironmentVariable("TranslationAPIUri", EnvironmentVariableTarget.Process);
    String translationKey = System.Environment.GetEnvironmentVariable("TranslationAPIKey", EnvironmentVariableTarget.Process);
    String translationLanguage = System.Environment.GetEnvironmentVariable("TranslationAPITargetLanguage", EnvironmentVariableTarget.Process);

    // Computer Vision Service
    String visionUri = System.Environment.GetEnvironmentVariable("VisionAPIUri", EnvironmentVariableTarget.Process);
    String visionKey = System.Environment.GetEnvironmentVariable("VisionAPIKey", EnvironmentVariableTarget.Process);

    // Face Service
    String faceUri = System.Environment.GetEnvironmentVariable("FaceAPIUri", EnvironmentVariableTarget.Process);
    String faceKey = System.Environment.GetEnvironmentVariable("FaceAPIKey", EnvironmentVariableTarget.Process);

    // Text Service
    String textUri = System.Environment.GetEnvironmentVariable("TextAPIUri", EnvironmentVariableTarget.Process);
    String textKey = System.Environment.GetEnvironmentVariable("TextAPIKey", EnvironmentVariableTarget.Process);

    // Just testing this.....
    using( DocumentClient client = CosmosInsertHelper.GetClientFromConnectionString("foobar") )
    {
        
    }
}
