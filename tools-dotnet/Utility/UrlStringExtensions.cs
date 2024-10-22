using System;
using System.Linq;

namespace tools_dotnet.Utility
{
    public static class UrlStringExtensions
    {
        public static string[] TrimEmptyUrlParts(this string[] urlParts)
        {
            return urlParts.Where(e => !string.IsNullOrEmpty(e.Trim().Trim('/'))).ToArray();
        }

        public static string? ExtractDomain(this string url)
        {
            var tmpUrl = url;

            if (!url.StartsWith("http://") && !url.StartsWith("https://"))
            {
                tmpUrl = "https://" + tmpUrl;
            }

            if (Uri.TryCreate(tmpUrl, UriKind.Absolute, out var createdUri))
            {
                return createdUri.Host;
            }

            return null;
        }

        public static string[] ExtractDomain(this string[] urlList)
        {
            return urlList.Select(ExtractDomain).Where(e => !string.IsNullOrEmpty(e)).Select(e => e!).ToArray();
        }
    }
}