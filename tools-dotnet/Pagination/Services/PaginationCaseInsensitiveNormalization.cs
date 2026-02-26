namespace tools_dotnet.Pagination.Services
{
    /// <summary>
    /// Defines how case-insensitive string operators normalize values.
    /// </summary>
    public enum PaginationCaseInsensitiveNormalization
    {
        /// <summary>
        /// Normalizes both sides using upper-case conversion.
        /// </summary>
        ToUpper = 0,

        /// <summary>
        /// Normalizes both sides using lower-case conversion.
        /// </summary>
        ToLower = 1,

        /// <summary>
        /// Does not normalize values and relies on database collation/type behavior.
        /// </summary>
        None = 2,
    }
}
