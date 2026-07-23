using Microsoft.AspNetCore.Http;
using Shouldly;
using tools_dotnet.Errors;
using tools_dotnet.Exceptions;
using tools_dotnet.Pagination.Models;
using tools_dotnet.Utility;

namespace tools_dotnet.Tests.PaginationTest
{
    [TestFixture]
    public class PaginationErrorMappingTests
    {
        [Test]
        public void MapExceptionToApiError_ShouldReturnPaginationError()
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Path = "/users";

            var error = httpContext.MapExceptionToApiError(
                InvalidPaginationFilterException.UnknownField("missing")
            );

            error.ShouldBeOfType<ApiPaginationError>();
            error!.Status.ShouldBe(StatusCodes.Status400BadRequest);
            error.Title.ShouldBe("Invalid pagination request");
            error.Extensions["parameter"].ShouldBe("filters");
            error.Extensions["field"].ShouldBe("missing");
            error.Extensions["errorCode"].ShouldBe(PaginationErrorCode.UnknownField.ToString());
        }

        [Test]
        public void MapExceptionToApiError_ShouldIncludeFilterFailureDetails()
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Path = "/users";

            var error = httpContext.MapExceptionToApiError(
                InvalidPaginationFilterException.UnsupportedOperator(
                    "age",
                    PaginationOperator.Contains,
                    typeof(int)
                )
            );

            error.ShouldBeOfType<ApiPaginationError>();
            error!.Extensions["parameter"].ShouldBe("filters");
            error.Extensions["field"].ShouldBe("age");
            error.Extensions["operator"].ShouldBe("@=");
            error.Extensions["targetType"].ShouldBe(nameof(Int32));
            error.Extensions["errorCode"].ShouldBe(
                PaginationErrorCode.UnsupportedOperator.ToString()
            );
            error.Extensions.ShouldNotContainKey("value");
        }

        [Test]
        public void MapExceptionToApiError_ShouldIncludeSortFailureDetails()
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Path = "/users";

            var error = httpContext.MapExceptionToApiError(
                InvalidPaginationSortException.FieldNotSortable("hidden")
            );

            error.ShouldBeOfType<ApiPaginationError>();
            error!.Extensions["parameter"].ShouldBe("sorts");
            error.Extensions["field"].ShouldBe("hidden");
            error.Extensions["errorCode"].ShouldBe(
                PaginationErrorCode.FieldNotSortable.ToString()
            );
            error.Extensions.ShouldNotContainKey("value");
            error.Extensions.ShouldNotContainKey("operator");
            error.Extensions.ShouldNotContainKey("targetType");
        }
    }
}
