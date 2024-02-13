using System.Collections.Generic;

namespace tools_dotnet.PropertyProcessor.Filter
{
    public class ArrayContainsFilter<T> : IFilter<ICollection<T>, T>
    {
        public bool Apply(ICollection<T> lhs, T rhs)
        {
            return lhs.Contains(rhs);
        }
    }
}