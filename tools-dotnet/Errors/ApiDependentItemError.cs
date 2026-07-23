namespace tools_dotnet.Errors
{
    /// <summary>Represents an API error caused by dependent data.</summary>
    public class ApiDependentItemError : GenericApiError
    {
        /// <summary>Initializes a new instance of <c>ApiDependentItemError</c>.</summary>
        protected ApiDependentItemError() { }

        /// <summary>Initializes a new instance of <c>ApiDependentItemError</c>.</summary>
        public ApiDependentItemError(string instance, string message)
            : base("Resource dependence", message, instance, System.Net.HttpStatusCode.Conflict) { }

        /// <summary>Creates an API error from a dependent-item exception.</summary>
        public static ApiDependentItemError CreateApiDependentItemError(
            string instance,
            bool onRemove
        )
        {
            return onRemove
                ? new ApiDependentItemError(
                    instance,
                    "The request could not be completed due to an existing resource, that depends on the current resource"
                )
                : new ApiDependentItemError(
                    instance,
                    "The request could not be completed due to a specified resource reference, that does not exist"
                );
        }
    }
}
