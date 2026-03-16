namespace tools_dotnet.Errors
{
    public class ApiConcurrentModificationError : GenericApiError
    {
        protected ApiConcurrentModificationError() { }

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
