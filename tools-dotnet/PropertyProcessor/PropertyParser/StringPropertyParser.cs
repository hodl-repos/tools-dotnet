using System;

namespace tools_dotnet.PropertyProcessor.PropertyParser
{
    public class StringPropertyParser : IPropertyParser<string>
    {
        public bool CanParse(string value)
        {
            return value.StartsWith("\"") && value.EndsWith("\"");
        }

        public string Parse(string value)
        {
            if (!CanParse(value))
            {
                throw new ArgumentOutOfRangeException("value", "cannot parse value, check before parsing");
            }

            return value
                .Remove(0, 0) //remove first "
                .Remove(value.Length - 1, 1) //remove last "
                .Replace("\\\"", "\""); //un-escape \" to "
        }
    }
}