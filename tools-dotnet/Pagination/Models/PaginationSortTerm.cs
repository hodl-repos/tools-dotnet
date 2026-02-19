using System;

namespace tools_dotnet.Pagination.Models
{
    /// <summary>
    /// A single parsed sort clause.
    /// </summary>
    public sealed class PaginationSortTerm
    {
        /// <summary>
        /// Creates a sort term.
        /// </summary>
        /// <param name="field">Field name to sort on.</param>
        /// <param name="descending">Set to <see langword="true"/> for descending sort.</param>
        public PaginationSortTerm(string field, bool descending)
        {
            if (string.IsNullOrWhiteSpace(field))
            {
                throw new ArgumentException("Field cannot be null or whitespace.", nameof(field));
            }

            Field = field;
            Descending = descending;
        }

        /// <summary>
        /// Gets field name used for sorting.
        /// </summary>
        public string Field { get; }

        /// <summary>
        /// Gets a value indicating whether sorting is descending.
        /// </summary>
        public bool Descending { get; }
    }
}