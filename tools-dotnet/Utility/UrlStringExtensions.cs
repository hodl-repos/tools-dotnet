using System;
using System.Linq;
using System.Net.Mail;
using System.Text.RegularExpressions;

namespace tools_dotnet.Utility
{
    public static class UrlStringExtensions
    {
        public static string[] TrimEmptyUrlParts(this string[] strings)
        {
            return strings.Where(e => !string.IsNullOrEmpty(e.Trim().Trim('/'))).ToArray();
        }
    }
}