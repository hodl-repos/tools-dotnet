using Microsoft.AspNetCore.Http;
using System;
using tools_dotnet.Errors;
using tools_dotnet.Exceptions;

namespace tools_dotnet.Utility
{
    public static class GenericErrorExtensions
    {
        /// <summary>
        /// maps known exceptions from tools_dotnet.Exceptions to tools_dotnet.Errors, when no exception matches, returns null
        /// </summary>
        public static GenericApiError? MapExceptionToApiError(HttpContext httpContext, Exception exception)
        {
            switch (exception)
            {
                case var _ when exception is FluentValidation.ValidationException validationException:
                    return new ApiValidationError(httpContext.Request.Path, validationException);

                case var _ when exception is ItemNotFoundException:
                    return new ApiItemNotFoundError(httpContext.Request.Path);

                case var _ when exception is ConflictingItemException:
                    return new ApiConflictingItemError(httpContext.Request.Path);

                case var _ when exception is NoPermissionException:
                    return new ApiNoPermissionError(httpContext.Request.Path);

                case var _ when exception is PaymentRequiredException:
                    return new ApiPaymentRequiredError(httpContext.Request.Path);

                case var _ when exception is DependentItemException dpEx:
                    return ApiDependentItemError.CreateApiDependentItemError(httpContext.Request.Path, dpEx.OnRemove);
            }

            return null;
        }
    }
}