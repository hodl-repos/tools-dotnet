using System.Linq;
using System.Net.Mail;

namespace tools_dotnet.Utility
{
    /// <summary>
    /// Provides extension methods for extracting and validating email addresses from strings.
    /// </summary>
    public static class EmailStringExtensions
    {
        /// <summary>
        /// extracts all e-mail addresses from a mail, may be filled directly with content from recipient, cc, bcc
        /// </summary>
        public static string[] ExtractEmailAdresses(params string?[] input)
        {
            return string.Join(
                    ',',
                    input
                        .Where(e => !string.IsNullOrEmpty(e?.Trim()))
                        .Select(e => e!.Trim())
                        .ToArray()
                )
                .ExtractEmailAdresses()
                .Distinct()
                .ToArray();
        }

        /// <summary>
        /// Extracts email addresses while removing display-name metadata such as text in
        /// &lt;angle brackets&gt;.
        /// </summary>
        public static string[] ExtractEmailAdresses(this string input)
        {
            input = input.Trim();

            if (string.IsNullOrEmpty(input))
            {
                return [];
            }

            var collection = new MailAddressCollection { input };

            return collection.Select(e => e.Address).ToArray();
        }

        /// <summary>Returns a normalized email address when the input can be parsed.</summary>
        public static string? TryExtractEmail(this string input)
        {
            var canParseEmail = MailAddress.TryCreate(input, out var result);

            if (canParseEmail)
            {
                return result!.Address;
            }

            return null;
        }
    }
}
