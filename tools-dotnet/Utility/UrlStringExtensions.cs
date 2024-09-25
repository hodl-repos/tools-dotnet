using System.Linq;

namespace tools_dotnet.Utility
{
    public static class UrlStringExtensions
    {
        public static string[] TrimEmptyUrlParts(this string[] urlParts)
        {
            return urlParts.Where(e => !string.IsNullOrEmpty(e.Trim().Trim('/'))).ToArray();
        }
    }
}