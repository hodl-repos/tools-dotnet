using System;
using System.Collections.Generic;
using System.Text;

namespace tools_dotnet.PropertyProcessor.Filter
{
    public class StringContainsFilter : IFilter<string, string>
    {
        public bool Apply(string lhs, string rhs)
        {
            return lhs.Contains(rhs);
        }
    }
}