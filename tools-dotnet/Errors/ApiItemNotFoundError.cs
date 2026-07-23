namespace tools_dotnet.Errors
{
    /// <summary>Represents an API error for a missing item.</summary>
    public class ApiItemNotFoundError : GenericApiError
    {
        /// <summary>Initializes a new instance of <c>ApiItemNotFoundError</c>.</summary>
        protected ApiItemNotFoundError() { }

        /// <summary>Initializes a new instance of <c>ApiItemNotFoundError</c>.</summary>
        public ApiItemNotFoundError(string instance)
            : base(
                "Resource not found",
                "Could not find the resource with the given identifier",
                instance,
                System.Net.HttpStatusCode.NotFound
            )
        { }
    }
}
