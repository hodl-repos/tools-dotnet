using System;
using System.Collections.Generic;
using tools_dotnet.Pagination.Models;

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
            string? filterTypeDisplayNameOverride = null,
            string source = "member",
            bool isDefaultSorted = false,
            bool defaultSortDescending = false
        )
        {
            Name = string.IsNullOrWhiteSpace(name)
                ? throw new ArgumentException(
                    "Field name cannot be null or whitespace.",
                    nameof(name)
                )
                : name;
            MemberType = memberType ?? throw new ArgumentNullException(nameof(memberType));
            CanFilter = canFilter;
            CanSort = canSort;
            Operators = operators ?? throw new ArgumentNullException(nameof(operators));
            FilterTypeDisplayNameOverride = filterTypeDisplayNameOverride;
            Source = string.IsNullOrWhiteSpace(source)
                ? throw new ArgumentException(
                    "Field source cannot be null or whitespace.",
                    nameof(source)
                )
                : source;
            IsDefaultSorted = isDefaultSorted;
            DefaultSortDescending = defaultSortDescending;
        }

        public string Name { get; }

        public Type MemberType { get; }

        public bool CanFilter { get; }

        public bool CanSort { get; }

        public IReadOnlyList<PaginationOperator> Operators { get; }

        public string? FilterTypeDisplayNameOverride { get; }

        public string Source { get; }

        public bool IsDefaultSorted { get; }

        public bool DefaultSortDescending { get; }
    }
}
