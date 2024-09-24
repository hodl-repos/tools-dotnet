using System.Linq;
using System.Net.Mail;

namespace tools_dotnet.Utility
{
    public static class EmailStringExtensions
    {
        public static string[] ExtractEmailAdresses(params string?[] input)
        {
            return string.Join(',', input.Where(e => !string.IsNullOrEmpty(e?.Trim())).Select(e => e!.Trim()).ToArray())
                    .ExtractEmailAdresses()
                    .Distinct()
                    .ToArray();
        }

        public static string[] ExtractEmailAdresses(this string input)
        {
            input = input.Trim();

            if (string.IsNullOrEmpty(input))
            {
                return [];
            }

            var collection = new MailAddressCollection
            {
                input
            };

            return collection.Select(e => e.Address).ToArray();
        }
    }
}