using tools_dotnet.Pagination.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace tools_dotnet.Pagination.Services
{
    /// <summary>
    /// Parses raw filter/sort text into strongly typed pagination terms.
    /// </summary>
    public class PaginationModelDeserializer : IPaginationModelDeserializer
    {
        private const int DefaultPage = 1;
        private const int DefaultPageSize = 25;
        private static readonly HashSet<char> EscapableCharacters = new()
        {
            ',',
            '|',
            '\\',
            '(',
            ')',
            '!',
            '@',
            '_',
            '=',
            '>',
            '<',
            '*'
        };

        /// <inheritdoc />
        public DeserializedPaginationModel Deserialize(PaginationModel model)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            var filters = ParseFilters(model.Filters);
            var sorts = ParseSorts(model.Sorts);
            var page = NormalizePage(model.Page);
            var pageSize = NormalizePageSize(model.PageSize);

            return new DeserializedPaginationModel(filters, sorts, page, pageSize);
        }

        private static IReadOnlyList<PaginationFilterTerm> ParseFilters(string? rawFilters)
        {
            if (string.IsNullOrWhiteSpace(rawFilters))
            {
                return Array.Empty<PaginationFilterTerm>();
            }

            var result = new List<PaginationFilterTerm>();
            var terms = SplitWithEscaping(rawFilters, ',');

            foreach (var rawTerm in terms)
            {
                var term = rawTerm.Trim();

                if (term.Length == 0)
                {
                    continue;
                }

                if (!TryFindOperator(term, out var operatorToken, out var operatorIndex))
                {
                    continue;
                }

                var rawFields = NormalizeFieldsSegment(term[..operatorIndex]);
                var rawValues = term[(operatorIndex + operatorToken!.Length)..];

                var fields = SplitWithEscaping(rawFields, '|')
                    .Select(x => Unescape(x).Trim())
                    .Where(x => x.Length > 0)
                    .ToArray();

                if (fields.Length == 0)
                {
                    continue;
                }

                if (!PaginationOperator.TryFromToken(operatorToken, out var @operator) || @operator == null)
                {
                    continue;
                }

                var values = SplitWithEscaping(rawValues, '|')
                    .Select(x => Unescape(x).Trim())
                    .ToArray();

                result.Add(new PaginationFilterTerm(fields, @operator, values));
            }

            return result;
        }

        private static IReadOnlyList<PaginationSortTerm> ParseSorts(string? rawSorts)
        {
            if (string.IsNullOrWhiteSpace(rawSorts))
            {
                return Array.Empty<PaginationSortTerm>();
            }

            var result = new List<PaginationSortTerm>();
            var terms = SplitWithEscaping(rawSorts, ',');

            foreach (var rawTerm in terms)
            {
                var term = rawTerm.Trim();

                if (term.Length == 0)
                {
                    continue;
                }

                var descending = term.StartsWith("-", StringComparison.Ordinal);

                if (descending || term.StartsWith("+", StringComparison.Ordinal))
                {
                    term = term[1..];
                }

                term = Unescape(term).Trim();

                if (term.Length == 0)
                {
                    continue;
                }

                result.Add(new PaginationSortTerm(term, descending));
            }

            return result;
        }

        private static int NormalizePage(int? page)
        {
            if (!page.HasValue || page.Value < 1)
            {
                return DefaultPage;
            }

            return page.Value;
        }

        private static int NormalizePageSize(int? pageSize)
        {
            if (!pageSize.HasValue || pageSize.Value < 1)
            {
                return DefaultPageSize;
            }

            return pageSize.Value;
        }

        private static bool TryFindOperator(string term, out string? operatorToken, out int operatorIndex)
        {
            operatorToken = null;
            operatorIndex = -1;

            foreach (var token in PaginationOperator.TokensByLength)
            {
                var index = term.IndexOf(token, StringComparison.Ordinal);

                if (index <= 0)
                {
                    continue;
                }

                if (operatorIndex == -1 || index < operatorIndex || (index == operatorIndex && token.Length > operatorToken!.Length))
                {
                    operatorIndex = index;
                    operatorToken = token;
                }
            }

            return operatorToken != null;
        }

        internal static IReadOnlyList<string> SplitWithEscaping(string input, char delimiter)
        {
            if (input.Length == 0)
            {
                return new[] { string.Empty };
            }

            var result = new List<string>();
            var sb = new StringBuilder();
            var escaped = false;

            for (var i = 0; i < input.Length; i++)
            {
                var c = input[i];

                if (escaped)
                {
                    sb.Append(c);
                    escaped = false;
                    continue;
                }

                if (c == '\\')
                {
                    var hasNext = i + 1 < input.Length;

                    if (hasNext && (input[i + 1] == delimiter || input[i + 1] == '\\'))
                    {
                        escaped = true;
                    }
                    else
                    {
                        sb.Append(c);
                    }

                    continue;
                }

                if (c == delimiter)
                {
                    result.Add(sb.ToString());
                    sb.Clear();
                    continue;
                }

                sb.Append(c);
            }

            if (escaped)
            {
                sb.Append('\\');
            }

            result.Add(sb.ToString());
            return result;
        }

        private static string Unescape(string value)
        {
            if (value.Length == 0)
            {
                return value;
            }

            var sb = new StringBuilder(value.Length);

            for (var i = 0; i < value.Length; i++)
            {
                var c = value[i];

                if (c == '\\' && i + 1 < value.Length && EscapableCharacters.Contains(value[i + 1]))
                {
                    sb.Append(value[i + 1]);
                    i++;
                    continue;
                }

                sb.Append(c);
            }

            return sb.ToString();
        }

        private static string NormalizeFieldsSegment(string rawFields)
        {
            var normalized = rawFields.Trim();

            if (normalized.Length >= 2 && normalized[0] == '(' && normalized[^1] == ')')
            {
                normalized = normalized[1..^1];
            }

            return normalized;
        }
    }
}
