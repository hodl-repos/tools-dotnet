using tools_dotnet.Pagination.Models;
using System;
using System.Collections.Generic;

namespace tools_dotnet.Pagination.OpenApi
{
    internal sealed class PaginationOpenApiFieldDescriptor
    {
        public PaginationOpenApiFieldDescriptor(
            string name,
            Type memberType,
            bool canFilter,
            bool canSort,
            IReadOnlyList<PaginationOperator> operators,
            string? filterTypeDisplayNameOverride = null)
        {
            Name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("Field name cannot be null or whitespace.", nameof(name)) : name;
            MemberType = memberType ?? throw new ArgumentNullException(nameof(memberType));
            CanFilter = canFilter;
            CanSort = canSort;
            Operators = operators ?? throw new ArgumentNullException(nameof(operators));
            FilterTypeDisplayNameOverride = filterTypeDisplayNameOverride;
        }

        public string Name { get; }

        public Type MemberType { get; }

        public bool CanFilter { get; }

        public bool CanSort { get; }

        public IReadOnlyList<PaginationOperator> Operators { get; }

        public string? FilterTypeDisplayNameOverride { get; }
    }
}
