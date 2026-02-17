using System;
using System.Collections.Generic;
using System.Linq;

namespace tools_dotnet.Pagination.OpenApi
{
    internal static class PaginationOpenApiDescriptionBuilder
    {
        public static string BuildFiltersDescription(IReadOnlyList<PaginationOpenApiFieldDescriptor> fields)
        {
            if (fields == null)
            {
                throw new ArgumentNullException(nameof(fields));
            }

            if (fields.Count == 0)
            {
                return string.Empty;
            }

            var lines = new List<string>
            {
                "Supported syntax: field{operator}value. Use ',' for AND and '|' for OR (fields and values).",
                "Escape special characters with '\\'. Use 'null' for null, and '\\null' for the literal text 'null'.",
                "Allowed filter fields:"
            };

            foreach (var field in fields.Where(x => x.CanFilter))
            {
                var operators = string.Join(", ", field.Operators.Select(x => x.Id));
                lines.Add($"- `{field.Name}` ({PaginationOpenApiMetadataProvider.GetTypeDisplayName(field.MemberType)}): {operators}");
            }

            return string.Join(Environment.NewLine, lines);
        }

        public static string BuildSortsDescription(IReadOnlyList<PaginationOpenApiFieldDescriptor> fields)
        {
            if (fields == null)
            {
                throw new ArgumentNullException(nameof(fields));
            }

            var sortableFields = fields
                .Where(x => x.CanSort)
                .Select(x => $"`{x.Name}`")
                .ToArray();

            if (sortableFields.Length == 0)
            {
                return string.Empty;
            }

            return
                $"Supported syntax: field for ascending, -field for descending, comma for multiple sorts.{Environment.NewLine}" +
                $"Allowed sort fields: {string.Join(", ", sortableFields)}";
        }
    }
}
