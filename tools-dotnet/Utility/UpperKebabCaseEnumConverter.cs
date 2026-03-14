using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace tools_dotnet.Utility
{
    public sealed class UpperKebabCaseEnumConverter<TEnum> : JsonStringEnumConverter
        where TEnum : struct
    {
        public UpperKebabCaseEnumConverter()
            : base(UpperKebabCaseNamingPolicy.Instance) { }
    }

    public sealed class UpperKebabCaseNamingPolicy : JsonNamingPolicy
    {
        public static UpperKebabCaseNamingPolicy Instance { get; } = new();

        public override string ConvertName(string name) =>
            string.IsNullOrEmpty(name) ? name : ToUpperKebabCase(name);

        private static string ToUpperKebabCase(string input)
        {
            var sb = new StringBuilder(input.Length + 8);

            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];

                // Normalize common separators to '-'
                if (c is '_' or ' ' or '.')
                {
                    AppendDashIfNeeded(sb);
                    continue;
                }

                // Insert dash on word boundaries:
                // - lower/digit -> upper  (myValue -> my-Value)
                // - acronym boundary: "HTTPServer" -> "HTTP-Server" (P followed by S + next is lower)
                if (i > 0 && char.IsLetterOrDigit(c))
                {
                    char prev = input[i - 1];
                    bool isUpper = char.IsUpper(c);
                    bool prevIsLowerOrDigit = char.IsLower(prev) || char.IsDigit(prev);

                    bool prevIsUpper = char.IsUpper(prev);
                    bool nextIsLower = (i + 1 < input.Length) && char.IsLower(input[i + 1]);

                    if ((isUpper && prevIsLowerOrDigit) || (isUpper && prevIsUpper && nextIsLower))
                        AppendDashIfNeeded(sb);
                }

                if (c == '-')
                {
                    AppendDashIfNeeded(sb);
                    continue;
                }

                sb.Append(char.ToUpperInvariant(c));
            }

            // Trim trailing dash if any
            if (sb.Length > 0 && sb[^1] == '-')
                sb.Length--;

            return sb.ToString();
        }

        private static void AppendDashIfNeeded(StringBuilder sb)
        {
            if (sb.Length == 0)
                return;
            if (sb[^1] != '-')
                sb.Append('-');
        }
    }
}
