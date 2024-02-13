using System;
using System.Collections.Generic;
using System.Text;

namespace tools_dotnet.PropertyProcessor.Filter
{
    public class StringFilter : IFilter<string, string>
    {
        private Func<string, string, bool> _action;

        public StringFilter(Func<string, string, bool> action)
        {
            _action = action;
        }

        public bool Apply(string lhs, string rhs)
        {
            return _action.Invoke(lhs, rhs);
        }
    }
}