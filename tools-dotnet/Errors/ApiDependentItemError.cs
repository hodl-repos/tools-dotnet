namespace tools_dotnet.Errors
{
    public class ApiDependentItemError : GenericApiError
    {
        protected ApiDependentItemError()
        { }

        public ApiDependentItemError(string instance, string message) : base("Resource dependence", message,
            instance, System.Net.HttpStatusCode.Conflict)
        {
        }

        public static ApiDependentItemError CreateApiDependentItemError(string instance, bool onRemove)
        {
            return onRemove
                ? new ApiDependentItemError(instance, "The request could not be completed due to an existing resource, that depends on the current resource")
                : new ApiDependentItemError(instance, "The request could not be completed due to a specified resource reference, that does not exist");
        }
    }
}