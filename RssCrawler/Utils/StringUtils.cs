using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace RssCrawler.Utils
{
    public static class StringUtils
    {
        public static bool IsUrl(string urlText)
        {
            return Uri.TryCreate(urlText, UriKind.Absolute, out Uri uriResult)
                && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        }

        public static string MD5Hash(string input)
        {
            StringBuilder hash = new StringBuilder();
            MD5CryptoServiceProvider md5provider = new MD5CryptoServiceProvider();
            byte[] bytes = md5provider.ComputeHash(new UTF8Encoding().GetBytes(input));

            for (int i = 0; i < bytes.Length; i++)
            {
                hash.Append(bytes[i].ToString("x2"));
            }
            return hash.ToString();
        }

        public static string UnsignString(string str)
        {
            Regex regex = new Regex("\\p{IsCombiningDiacriticalMarks}+");
            string temp = str.Normalize(NormalizationForm.FormD);
            return regex.Replace(temp, String.Empty)
                        .Replace('\u0111', 'd').Replace('\u0110', 'D');
        }

        public static string RemoveNonAlphaCharactersAndDigit(string originText)
        {
            if (string.IsNullOrWhiteSpace(originText))
            {
                return "";
            }

            Regex rgx = new Regex("[^a-zA-Z]");
            return rgx.Replace(originText, "");
        }
    }
}
