namespace tools_dotnet.Errors
{
    public class ApiItemNotFoundError : GenericApiError
    {
        protected ApiItemNotFoundError()
        { }

        public ApiItemNotFoundError(string instance) : base(
            "Resource not found",
            "Could not find the resource with the given identifier",
            instance,
            System.Net.HttpStatusCode.NotFound)
        {
        }
    }
}