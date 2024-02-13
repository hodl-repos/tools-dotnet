using System;

namespace tools_dotnet.PropertyProcessor.Filter
{
    public class EqualFilter<T> : IFilter<T, T> where T : IEquatable<T>
    {
        public bool Apply(T lhs, T rhs)
        {
            return lhs.Equals(rhs);
        }
    }
}