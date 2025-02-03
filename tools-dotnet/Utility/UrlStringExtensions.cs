using System;
using System.Linq;

namespace tools_dotnet.Utility
{
    public static class UrlStringExtensions
    {
        public static string ResolveAndSanitizeWebUrl(string baseUrl, string path)
        {
            Uri baseUri = new Uri(baseUrl);
            Uri fullUri = new Uri(baseUri, path);

            return SanitizeWebUrl(fullUri.ToString());
        }

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

        public static string SanitizeWebUrl(this string url)
        {
            if (string.IsNullOrEmpty(url))
                return url;

            if (url.StartsWith("//"))
            {
                // If URL starts with "//", replace with "http://"
                return "https:" + url;
            }
            else if (!url.StartsWith("http://") && !url.StartsWith("https://"))
            {
                // If URL doesn't have a protocol, prepend "http://"
                return "https://" + url;
            }

            return url;
        }

        public static string RemoveQueryParams(this string url)
        {
            if (string.IsNullOrEmpty(url))
                return url;

            try
            {
                Uri uri = new Uri(url);
                // Reconstruct the URL without the query
                return uri.GetLeftPart(UriPartial.Path);
            }
            catch (UriFormatException)

            {
                // If the input is not a valid URI, return it unchanged
                return url;
            }
        }
    }
}