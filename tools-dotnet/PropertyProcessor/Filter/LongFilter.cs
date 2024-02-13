using System;
using System.Collections.Generic;
using System.Text;

namespace tools_dotnet.PropertyProcessor.Filter
{
    public class LongFilter : IFilter<long, long>
    {
        private Func<long, long, bool> _action;

        public LongFilter(Func<long, long, bool> action)
        {
            _action = action;
        }

        public bool Apply(long lhs, long rhs)
        {
            return _action.Invoke(lhs, rhs);
        }
    }
}