using System;
using tools_dotnet.Pagination.Models;

namespace tools_dotnet.Exceptions
{
    /// <summary>
    /// Exception thrown when the filters query parameter is invalid.
    /// </summary>
    public sealed class InvalidPaginationFilterException : PaginationException
    {
        private const string Parameter = "filters";

        private InvalidPaginationFilterException(
            string message,
            PaginationErrorCode errorCode,
            string? field = null,
            string? value = null,
            string? @operator = null,
            Type? targetType = null,
            Exception? innerException = null
        )
            : base(message, Parameter, errorCode, field, value, @operator, targetType, innerException) { }

        /// <summary>
        /// Creates an exception for invalid filter syntax.
        /// </summary>
        public static InvalidPaginationFilterException InvalidSyntax(string rawTerm)
        {
            return new InvalidPaginationFilterException(
                $"Filter is not valid. Could not parse filter term '{rawTerm}'.",
                PaginationErrorCode.InvalidSyntax,
                value: rawTerm
            );
        }

        /// <summary>
        /// Creates an exception for an unknown filter field.
        /// </summary>
        public static InvalidPaginationFilterException UnknownField(
            string field,
            string? rawTerm = null
        )
        {
            return new InvalidPaginationFilterException(
                $"Filter is not valid. Unknown property '{field}'.",
                PaginationErrorCode.UnknownField,
                field: field,
                value: rawTerm
            );
        }

        /// <summary>
        /// Creates an exception for a field that cannot be used in filters.
        /// </summary>
        public static InvalidPaginationFilterException FieldNotFilterable(string field)
        {
            return new InvalidPaginationFilterException(
                $"Filter is not valid. Property '{field}' is not filterable.",
                PaginationErrorCode.FieldNotFilterable,
                field: field
            );
        }

        /// <summary>
        /// Creates an exception for a filter value that cannot be parsed.
        /// </summary>
        public static InvalidPaginationFilterException FailedToParseValue(
            string field,
            string value,
            Type targetType
        )
        {
            return new InvalidPaginationFilterException(
                $"Filter is not valid. Value '{value}' for property '{field}' could not be parsed as '{targetType.Name}'.",
                PaginationErrorCode.FailedToParseValue,
                field: field,
                value: value,
                targetType: targetType
            );
        }

        /// <summary>
        /// Creates an exception for an operator that cannot be used with a filter field.
        /// </summary>
        public static InvalidPaginationFilterException UnsupportedOperator(
            string field,
            PaginationOperator op,
            Type targetType
        )
        {
            return new InvalidPaginationFilterException(
                $"Filter is not valid. Operator '{op.Id}' cannot be used for property '{field}' of type '{targetType.Name}'.",
                PaginationErrorCode.UnsupportedOperator,
                field: field,
                @operator: op.Id,
                targetType: targetType
            );
        }
    }
}
