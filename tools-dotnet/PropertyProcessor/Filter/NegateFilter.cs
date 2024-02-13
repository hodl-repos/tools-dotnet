namespace tools_dotnet.PropertyProcessor.Filter
{
    public class NegateFilter<LHS, RHS> : IFilter<LHS, RHS>
    {
        private readonly IFilter<LHS, RHS> _filter;

        public NegateFilter(IFilter<LHS, RHS> filter)
        {
            _filter = filter;
        }

        public bool Apply(LHS lhs, RHS rhs)
        {
            return !_filter.Apply(lhs, rhs);
        }
    }
}