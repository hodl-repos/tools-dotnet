namespace tools_dotnet.Errors
{
    public class ApiConflictingItemError : GenericApiError
    {
        protected ApiConflictingItemError()
        { }

        public ApiConflictingItemError(string instance) : base(
            "Resource conflict",
            "The request could not be completed due to a conflict with an already existing resource, that uses the same unique identifier(s)",
            instance,
            System.Net.HttpStatusCode.Conflict)
        {
        }
    }
}