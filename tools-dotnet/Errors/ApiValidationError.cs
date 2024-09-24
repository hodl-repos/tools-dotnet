using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace tools_dotnet.Errors
{
    public class ApiValidationError : GenericApiError
    {
        public IEnumerable<ApiPropertyValidationFailure> Errors { get; set; } = new List<ApiPropertyValidationFailure>();

        protected ApiValidationError()
        { }

        public ApiValidationError(FluentValidation.ValidationException ex) : base("One or more validation errors occurred",
            "Please refer to the errors property for additional details", HttpStatusCode.BadRequest)
        {
            var errorList = new List<ApiPropertyValidationFailure>();

            if (ex.Errors.Any())
            {
                foreach (var item in ex.Errors)
                {
                    errorList.Add(new ApiPropertyValidationFailure(item.PropertyName, item.ErrorMessage));
                }
            }
            else
            {
                Detail = ex.Message;
            }

            Errors = errorList;
        }

        public ApiValidationError(string instance, FluentValidation.ValidationException ex) : this(ex)
        {
            Instance = instance;
        }

        public ApiValidationError(string instance, IEnumerable<ApiPropertyValidationFailure> errors) : base("One or more validation errors occurred",
            "Please refer to the errors property for additional details", instance, HttpStatusCode.BadRequest)
        {
            Errors = errors;
        }

        public class ApiPropertyValidationFailure
        {
            public string PropertyName { get; set; }
            public string ErrorMessage { get; set; }

            public ApiPropertyValidationFailure(string propertyName, string errorMessage)
            {
                PropertyName = propertyName;
                ErrorMessage = errorMessage;
            }
        }
    }
}