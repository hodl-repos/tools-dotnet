namespace tools_dotnet.PropertyProcessor
{
    public interface IFilter
    {
    }

    public interface IFilter<LHS, RHS> : IFilter
    {
        bool Apply(LHS lhs, RHS rhs);
    }
}