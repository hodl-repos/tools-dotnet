using System;
using Microsoft.AspNetCore.Http;
using tools_dotnet.Errors;
using tools_dotnet.Exceptions;

namespace tools_dotnet.Utility
{
    /// <summary>
    /// Provides extension methods for mapping exceptions to generic API errors.
    /// </summary>
    public static class GenericErrorExtensions
    {
        /// <summary>
        /// maps known exceptions from tools_dotnet.Exceptions to tools_dotnet.Errors, when no exception matches, returns null
        /// </summary>
        public static GenericApiError? MapExceptionToApiError(
            this HttpContext httpContext,
            Exception exception
        )
        {
            switch (exception)
            {
                case FluentValidation.ValidationException validationException:
                    return new ApiValidationError(httpContext.Request.Path, validationException);

                case ItemNotFoundException:
                    return new ApiItemNotFoundError(httpContext.Request.Path);

                case ConflictingItemException:
                    return new ApiConflictingItemError(httpContext.Request.Path);

                case NoPermissionException:
                    return new ApiNoPermissionError(httpContext.Request.Path);

                case PaymentRequiredException:
                    return new ApiPaymentRequiredError(httpContext.Request.Path);

                case ConcurrentModificationException concurrencyException:
                    return new ApiConcurrentModificationError(
                        httpContext.Request.Path,
                        concurrencyException.DbConcurrencyStamp,
                        concurrencyException.RequestConcurrencyStamp
                    );

                case PaginationException paginationException:
                    return new ApiPaginationError(httpContext.Request.Path, paginationException);

                case DependentItemException dependentItemException:
                    return ApiDependentItemError.CreateApiDependentItemError(
                        httpContext.Request.Path,
                        dependentItemException.OnRemove
                    );
            }

            return null;
        }
    }
}
