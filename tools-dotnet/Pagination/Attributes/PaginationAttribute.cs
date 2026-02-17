using System;

namespace tools_dotnet.Pagination.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class PaginationAttribute : Attribute
    {
        public bool CanFilter { get; set; } = true;

        public bool CanSort { get; set; } = true;

        public string? Name { get; set; }
    }
}
