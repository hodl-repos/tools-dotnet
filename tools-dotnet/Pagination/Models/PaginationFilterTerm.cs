using System;
using System.Collections.Generic;
using System.Linq;

namespace tools_dotnet.Pagination.Models
{
    public sealed class PaginationFilterTerm
    {
        public PaginationFilterTerm(IReadOnlyList<string> fields, PaginationOperator @operator, IReadOnlyList<string> values)
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

        public IReadOnlyList<string> Fields { get; }

        public PaginationOperator Operator { get; }

        public IReadOnlyList<string> Values { get; }
    }
}
