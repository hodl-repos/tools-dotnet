namespace tools_dotnet.Exceptions
{
    /// <summary>
    /// Exception thrown when the sorts query parameter is invalid.
    /// </summary>
    public sealed class InvalidPaginationSortException : PaginationException
    {
        private const string Parameter = "sorts";

        private InvalidPaginationSortException(
            string message,
            PaginationErrorCode errorCode,
            string? field = null,
            string? value = null
        )
            : base(message, Parameter, errorCode, field, value) { }

        /// <summary>
        /// Creates an exception for an unknown sort field.
        /// </summary>
        public static InvalidPaginationSortException UnknownField(string field)
        {
            return new InvalidPaginationSortException(
                $"Sort is not valid. Unknown property '{field}'.",
                PaginationErrorCode.UnknownField,
                field: field
            );
        }

        /// <summary>
        /// Creates an exception for a field that cannot be used in sorts.
        /// </summary>
        public static InvalidPaginationSortException FieldNotSortable(string field)
        {
            return new InvalidPaginationSortException(
                $"Sort is not valid. Property '{field}' is not sortable.",
                PaginationErrorCode.FieldNotSortable,
                field: field
            );
        }
    }
}
