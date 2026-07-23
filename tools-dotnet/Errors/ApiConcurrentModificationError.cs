namespace tools_dotnet.Errors
{
    /// <summary>Represents an API error caused by an optimistic concurrency conflict.</summary>
    public class ApiConcurrentModificationError : GenericApiError
    {
        /// <summary>Initializes a new instance of <c>ApiConcurrentModificationError</c>.</summary>
        protected ApiConcurrentModificationError() { }

        /// <summary>Initializes a new instance of <c>ApiConcurrentModificationError</c>.</summary>
        public ApiConcurrentModificationError(
            string instance,
            string? dbConcurrencyStamp = null,
            string? requestConcurrencyStamp = null
        )
            : base(
                "Concurrent modification",
                "The resource has been modified by another process. Reload it and retry your changes.",
                instance,
                System.Net.HttpStatusCode.Conflict
            )
        {
            if (!string.IsNullOrWhiteSpace(dbConcurrencyStamp))
            {
                Extensions["dbConcurrencyStamp"] = dbConcurrencyStamp;
            }

            if (!string.IsNullOrWhiteSpace(requestConcurrencyStamp))
            {
                Extensions["requestConcurrencyStamp"] = requestConcurrencyStamp;
            }
        }
    }
}
