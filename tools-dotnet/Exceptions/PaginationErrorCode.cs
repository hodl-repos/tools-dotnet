namespace tools_dotnet.Exceptions
{
    /// <summary>
    /// Identifies the reason a pagination parameter is invalid.
    /// </summary>
    public enum PaginationErrorCode
    {
        /// <summary>
        /// The filter or sort syntax is invalid.
        /// </summary>
        InvalidSyntax,

        /// <summary>
        /// The requested field cannot be mapped.
        /// </summary>
        UnknownField,

        /// <summary>
        /// The requested field exists but cannot be used in filters.
        /// </summary>
        FieldNotFilterable,

        /// <summary>
        /// The requested field exists but cannot be used in sorts.
        /// </summary>
        FieldNotSortable,

        /// <summary>
        /// A raw filter value could not be converted to the target member type.
        /// </summary>
        FailedToParseValue,

        /// <summary>
        /// The requested operator cannot be used with the target member type.
        /// </summary>
        UnsupportedOperator,
    }
}
