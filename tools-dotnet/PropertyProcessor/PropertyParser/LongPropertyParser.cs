using System;
using System.Linq;

namespace tools_dotnet.PropertyProcessor.PropertyParser
{
    public class LongPropertyParser : IPropertyParser<long>
    {
        public bool CanParse(string value)
        {
            return value.All(char.IsDigit);
        }

        public long Parse(string value)
        {
            if (!CanParse(value))
            {
                throw new ArgumentOutOfRangeException("value", "cannot parse value, check before parsing");
            }

            return long.Parse(value);
        }
    }
}