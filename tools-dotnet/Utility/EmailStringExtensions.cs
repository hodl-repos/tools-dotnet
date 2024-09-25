using System.Linq;
using System.Net.Mail;

namespace tools_dotnet.Utility
{
    public static class EmailStringExtensions
    {
        /// <summary>
        /// extracts all e-mail addresses from a mail, may be filled directly with content from recipient, cc, bcc
        /// </summary>
        public static string[] ExtractEmailAdresses(params string?[] input)
        {
            return string.Join(',', input.Where(e => !string.IsNullOrEmpty(e?.Trim())).Select(e => e!.Trim()).ToArray())
                    .ExtractEmailAdresses()
                    .Distinct()
                    .ToArray();
        }

        /// <summary>
        /// extracts a single e-mail addresses, removing all metadata like beautified name withing <>
        /// </summary>
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