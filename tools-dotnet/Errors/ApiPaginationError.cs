using System.Net;
using tools_dotnet.Exceptions;

namespace tools_dotnet.Errors
{
    /// <summary>
    /// API error returned for invalid pagination query parameters.
    /// </summary>
    public class ApiPaginationError : GenericApiError
    {
        /// <summary>Initializes a new instance of <c>ApiPaginationError</c>.</summary>
        protected ApiPaginationError() { }

        /// <summary>
        /// Creates an API pagination error from a pagination exception.
        /// </summary>
        public ApiPaginationError(string instance, PaginationException exception)
            : base(
                "Invalid pagination request",
                exception.Message,
                instance,
                HttpStatusCode.BadRequest
            )
        {
            Extensions["parameter"] = exception.ParameterName;
            Extensions["errorCode"] = exception.ErrorCode.ToString();

            if (exception.Field != null)
            {
                Extensions["field"] = exception.Field;
            }

            if (exception.Value != null)
            {
                Extensions["value"] = exception.Value;
            }

            if (exception.Operator != null)
            {
                Extensions["operator"] = exception.Operator;
            }

            if (exception.TargetType != null)
            {
                Extensions["targetType"] = exception.TargetType.Name;
            }
        }
    }
}
