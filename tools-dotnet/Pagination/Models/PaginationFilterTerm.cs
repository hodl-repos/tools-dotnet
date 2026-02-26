using System;
using System.Collections.Generic;
using System.Linq;

namespace tools_dotnet.Pagination.Models
{
    /// <summary>
    /// A single parsed filter clause.
    /// </summary>
    public sealed class PaginationFilterTerm
    {
        /// <summary>
        /// Creates a filter term.
        /// </summary>
        /// <param name="fields">Fields included in this term. Multiple fields are OR-ed.</param>
        /// <param name="operator">Operator for the comparison.</param>
        /// <param name="values">Values included in this term. Multiple values are OR-ed for positive operators.</param>
        public PaginationFilterTerm(
            IReadOnlyList<string> fields,
            PaginationOperator @operator,
            IReadOnlyList<string> values
        )
        {
            if (fields == null)
            {
                throw new ArgumentNullException(nameof(fields));
            }

            if (fields.Count == 0)
            {
                throw new ArgumentException("At least one field must be provided.", nameof(fields));
            }

            if (fields.Any(string.IsNullOrWhiteSpace))
            {
                throw new ArgumentException("Fields cannot be null or whitespace.", nameof(fields));
            }

            Fields = fields;
            Operator = @operator ?? throw new ArgumentNullException(nameof(@operator));
            Values = values ?? throw new ArgumentNullException(nameof(values));
        }

        /// <summary>
        /// Gets fields used by this term.
        /// </summary>
        public IReadOnlyList<string> Fields { get; }

        /// <summary>
        /// Gets operator used by this term.
        /// </summary>
        public PaginationOperator Operator { get; }

        /// <summary>
        /// Gets values used by this term.
        /// </summary>
        public IReadOnlyList<string> Values { get; }
    }
}
