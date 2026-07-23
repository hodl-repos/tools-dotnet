using FluentValidation;
using Microsoft.AspNetCore.Http;
using Shouldly;
using tools_dotnet.Errors;
using tools_dotnet.Exceptions;
using tools_dotnet.Utility;

namespace tools_dotnet.Tests.UtilityTest
{
    [TestFixture]
    public class GenericErrorExtensionsTests
    {
        [TestCaseSource(nameof(KnownExceptions))]
        public void MapExceptionToApiError_ShouldMapKnownException(
            Exception exception,
            Type expectedErrorType,
            int expectedStatus
        )
        {
            var context = new DefaultHttpContext();
            context.Request.Path = "/items/42";

            var error = context.MapExceptionToApiError(exception);

            error.ShouldNotBeNull();
            error.GetType().ShouldBe(expectedErrorType);
            error.Status.ShouldBe(expectedStatus);
            error.Instance.ShouldBe("/items/42");
        }

        [Test]
        public void MapExceptionToApiError_ShouldPreserveConcurrencyDetails()
        {
            var context = new DefaultHttpContext();

            var error = context.MapExceptionToApiError(
                new ConcurrentModificationException("database", "request")
            );

            error.ShouldBeOfType<ApiConcurrentModificationError>();
            error!.Extensions["dbConcurrencyStamp"].ShouldBe("database");
            error.Extensions["requestConcurrencyStamp"].ShouldBe("request");
        }

        [Test]
        public void MapExceptionToApiError_ShouldReturnNullForUnknownException()
        {
            new DefaultHttpContext()
                .MapExceptionToApiError(new InvalidOperationException())
                .ShouldBeNull();
        }

        public static IEnumerable<TestCaseData> KnownExceptions()
        {
            yield return new TestCaseData(
                new ValidationException("invalid"),
                typeof(ApiValidationError),
                StatusCodes.Status400BadRequest
            );
            yield return new TestCaseData(
                new ItemNotFoundException("missing"),
                typeof(ApiItemNotFoundError),
                StatusCodes.Status404NotFound
            );
            yield return new TestCaseData(
                new ConflictingItemException(),
                typeof(ApiConflictingItemError),
                StatusCodes.Status409Conflict
            );
            yield return new TestCaseData(
                new NoPermissionException(),
                typeof(ApiNoPermissionError),
                StatusCodes.Status403Forbidden
            );
            yield return new TestCaseData(
                new PaymentRequiredException(),
                typeof(ApiPaymentRequiredError),
                StatusCodes.Status402PaymentRequired
            );
            yield return new TestCaseData(
                new ConcurrentModificationException("database", "request"),
                typeof(ApiConcurrentModificationError),
                StatusCodes.Status409Conflict
            );
            yield return new TestCaseData(
                new DependentItemException(onRemove: true),
                typeof(ApiDependentItemError),
                StatusCodes.Status409Conflict
            );
        }
    }
}
