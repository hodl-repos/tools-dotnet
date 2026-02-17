using System;

namespace tools_dotnet.Pagination.Models
{
    public sealed class PaginationSortTerm
    {
        public PaginationSortTerm(string field, bool descending)
        {
            if (string.IsNullOrWhiteSpace(field))
            {
                throw new ArgumentException("Field cannot be null or whitespace.", nameof(field));
            }

            Field = field;
            Descending = descending;
        }

        public string Field { get; }

        public bool Descending { get; }
    }
}
