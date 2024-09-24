namespace tools_dotnet.Errors
{
    public class ApiNoPermissionError : GenericApiError
    {
        protected ApiNoPermissionError()
        { }

        public ApiNoPermissionError(string instance) : base(
            "No permission",
            "You have no permission for this action",
            instance,
            System.Net.HttpStatusCode.Forbidden)
        {
        }
    }
}