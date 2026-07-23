namespace tools_dotnet.Errors
{
    /// <summary>Represents an API authorization error.</summary>
    public class ApiNoPermissionError : GenericApiError
    {
        /// <summary>Initializes a new instance of <c>ApiNoPermissionError</c>.</summary>
        protected ApiNoPermissionError() { }

        /// <summary>Initializes a new instance of <c>ApiNoPermissionError</c>.</summary>
        public ApiNoPermissionError(string instance)
            : base(
                "No permission",
                "You have no permission for this action",
                instance,
                System.Net.HttpStatusCode.Forbidden
            )
        { }
    }
}
