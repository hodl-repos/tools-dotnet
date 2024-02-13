using System;
using System.Collections.Generic;
using System.Text;

namespace tools_dotnet.PropertyProcessor.Filter
{
    public class StringArrayFilterLhs : IFilter<ICollection<string>, string>
    {
        private readonly IFilter<string, string> _filter;

        public StringArrayFilterLhs(IFilter<string, string> filter)
        {
            _filter = filter;
        }

        public bool Apply(ICollection<string> lhs, string rhs)
        {
            foreach (var lhsItem in lhs)
            {
                if (_filter.Apply(lhsItem, rhs))
                {
                    return true;
                }
            }
            return false;
        }
    }

    public class StringArrayFilterRhs : IFilter<string, ICollection<string>>
    {
        private readonly IFilter<string, string> _filter;

        public StringArrayFilterRhs(IFilter<string, string> filter)
        {
            _filter = filter;
        }

        public bool Apply(string lhs, ICollection<string> rhs)
        {
            foreach (var rhsItem in rhs)
            {
                if (_filter.Apply(lhs, rhsItem))
                {
                    return true;
                }
            }
            return false;
        }
    }

    public class StringArrayFilterBoth : IFilter<ICollection<string>, ICollection<string>>
    {
        private readonly IFilter<string, string> _filter;

        public StringArrayFilterBoth(IFilter<string, string> filter)
        {
            _filter = filter;
        }

        public bool Apply(ICollection<string> lhs, ICollection<string> rhs)
        {
            foreach (var lhsItem in lhs)
            {
                foreach (var rhsItem in rhs)
                {
                    if (_filter.Apply(lhsItem, rhsItem))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}