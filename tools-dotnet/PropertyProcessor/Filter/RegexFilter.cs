using System.Text.RegularExpressions;

namespace tools_dotnet.PropertyProcessor.Filter
{
    public class RegexFilter : IFilter<string, Regex>
    {
        public bool Apply(string lhs, Regex rhs)
        {
            return rhs.IsMatch(lhs);
        }
    }
}