using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace tools_dotnet.Errors
{
    /// <summary>Represents validation failures returned by an API.</summary>
    public class ApiValidationError : GenericApiError
    {
        /// <summary>Gets or sets the individual property validation failures.</summary>
        public IEnumerable<ApiPropertyValidationFailure> Errors { get; set; } =
            new List<ApiPropertyValidationFailure>();

        /// <summary>Initializes a new instance of <c>ApiValidationError</c>.</summary>
        protected ApiValidationError() { }

        /// <summary>Initializes a new instance of <c>ApiValidationError</c>.</summary>
        public ApiValidationError(FluentValidation.ValidationException ex)
            : base(
                "One or more validation errors occurred",
                "Please refer to the errors property for additional details",
                HttpStatusCode.BadRequest
            )
        {
            var errorList = new List<ApiPropertyValidationFailure>();

            if (ex.Errors.Any())
            {
                foreach (var item in ex.Errors)
                {
                    errorList.Add(
                        new ApiPropertyValidationFailure(item.PropertyName, item.ErrorMessage)
                    );
                }
            }
            else
            {
                Detail = ex.Message;
            }

            Errors = errorList;
        }

        /// <summary>Initializes a new instance of <c>ApiValidationError</c>.</summary>
        public ApiValidationError(string instance, FluentValidation.ValidationException ex)
            : this(ex)
        {
            Instance = instance;
        }

        /// <summary>Initializes a new instance of <c>ApiValidationError</c>.</summary>
        public ApiValidationError(string instance, IEnumerable<ApiPropertyValidationFailure> errors)
            : base(
                "One or more validation errors occurred",
                "Please refer to the errors property for additional details",
                instance,
                HttpStatusCode.BadRequest
            )
        {
            Errors = errors;
        }

        /// <summary>Represents one property-level validation failure.</summary>
        public class ApiPropertyValidationFailure
        {
            /// <summary>Gets or sets the invalid property name.</summary>
            public string PropertyName { get; set; }
            /// <summary>Gets or sets the validation error message.</summary>
            public string ErrorMessage { get; set; }

            /// <summary>Initializes a validation failure for one property.</summary>
            public ApiPropertyValidationFailure(string propertyName, string errorMessage)
            {
                PropertyName = propertyName;
                ErrorMessage = errorMessage;
            }
        }
    }
}
