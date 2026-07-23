namespace tools_dotnet.Enum
{
    /// <summary>Defines values used by <c>SoftDeleteQueryMode</c>.</summary>
    public enum SoftDeleteQueryMode
    {
        /// <summary>Includes only active entities.</summary>
        ActiveOnly = 0,
        /// <summary>Includes active and deleted entities.</summary>
        IncludeDeleted = 1,
        /// <summary>Includes only deleted entities.</summary>
        DeletedOnly = 2,
    }
}

