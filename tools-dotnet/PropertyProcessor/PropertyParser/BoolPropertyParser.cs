using System;
using System.Linq;

namespace tools_dotnet.PropertyProcessor.PropertyParser
{
    public class BoolPropertyParser : IPropertyParser<bool>
    {
        public bool CanParse(string value)
        {
            return bool.TryParse(value, out _);
        }

        public bool Parse(string value)
        {
            if (!CanParse(value))
            {
                throw new ArgumentOutOfRangeException("value", "cannot parse value, check before parsing");
            }

            return bool.Parse(value);
        }
    }
}