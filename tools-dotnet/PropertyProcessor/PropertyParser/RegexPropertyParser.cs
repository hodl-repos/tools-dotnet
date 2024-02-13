using System;
using System.Text.RegularExpressions;

namespace tools_dotnet.PropertyProcessor.PropertyParser
{
    public class RegexPropertyParser : IPropertyParser<Regex>
    {
        public bool CanParse(string value)
        {
            return value.StartsWith("R\"") && value.EndsWith("\"");
        }

        public Regex Parse(string value)
        {
            if (!CanParse(value))
            {
                throw new ArgumentOutOfRangeException("value", "cannot parse value, check before parsing");
            }

            return new Regex(value
                .Remove(0, 2) //remove first "
                .Remove(value.Length - 3, 1) //remove last "
                .Replace("\\\"", "\"")); //un-escape \" to "
        }
    }
}