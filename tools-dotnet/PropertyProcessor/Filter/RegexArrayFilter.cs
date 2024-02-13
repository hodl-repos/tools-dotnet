using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace tools_dotnet.PropertyProcessor.Filter
{
    public class RegexArrayFilter : IFilter<ICollection<string>, Regex>
    {
        public bool Apply(ICollection<string> lhs, Regex rhs)
        {
            foreach (var lhsItem in lhs)
            {
                if (rhs.IsMatch(lhsItem))
                {
                    return true;
                }
            }

            return false;
        }
    }
}