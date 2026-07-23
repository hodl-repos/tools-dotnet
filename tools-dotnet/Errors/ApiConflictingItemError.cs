namespace tools_dotnet.Errors
{
    /// <summary>Represents an API error caused by a conflicting item.</summary>
    public class ApiConflictingItemError : GenericApiError
    {
        /// <summary>Initializes a new instance of <c>ApiConflictingItemError</c>.</summary>
        protected ApiConflictingItemError() { }

        /// <summary>Initializes a new instance of <c>ApiConflictingItemError</c>.</summary>
        public ApiConflictingItemError(string instance)
            : base(
                "Resource conflict",
                "The request could not be completed due to a conflict with an already existing resource, that uses the same unique identifier(s)",
                instance,
                System.Net.HttpStatusCode.Conflict
            )
        { }
    }
}
