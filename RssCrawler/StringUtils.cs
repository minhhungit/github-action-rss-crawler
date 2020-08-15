using System;
using System.Security.Cryptography;
using System.Text;

namespace RssCrawler
{
    public static class StringUtils
    {
        public static bool IsUrl(string urlText)
        {
            return Uri.TryCreate(urlText, UriKind.Absolute, out Uri uriResult)
                && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        }
    }
}
