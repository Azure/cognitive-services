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

namespace RssGenerator.RSS
{
    /// <summary>
    /// A simple hash generation class used to make mock hashes on articles and images. 
    /// 
    /// Suggestion: Create a better solution for your enterprise grade solution.
    /// </summary>
    class HashGenerator
    {
        public static System.Security.Cryptography.SHA256Managed HashAlgorithm { get; private set; }

        public static String GetHash(String content)
        {
            if (String.IsNullOrEmpty(content))
                return String.Empty;

            HashGenerator.GetHashAlgorithm();
            byte[] textData = System.Text.Encoding.UTF8.GetBytes(content);
            byte[] hash = HashGenerator.HashAlgorithm.ComputeHash(textData);
            return BitConverter.ToString(hash).Replace("-", String.Empty);
        }

        public static String GetHash(byte[] content)
        {
            if (content == null || content.Length == 0)
                return String.Empty;

            HashGenerator.GetHashAlgorithm();
            byte[] hash = HashGenerator.HashAlgorithm.ComputeHash(content);
            return BitConverter.ToString(hash).Replace("-", String.Empty);
        }

        private static void GetHashAlgorithm()
        {
            if(HashGenerator.HashAlgorithm == null)
            {
                HashGenerator.HashAlgorithm = new System.Security.Cryptography.SHA256Managed();
            }
        }
    }
}
