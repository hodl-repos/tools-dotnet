using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace tools_dotnet.Pagination.OpenApi
{
    /// <summary>
    /// Enriches pagination query parameters in OpenAPI docs based on <see cref="Attributes.PaginationAttribute"/>.
    /// </summary>
    public sealed class PaginationOpenApiOperationTransformer : IOpenApiOperationTransformer
    {
        /// <summary>
        /// Updates operation metadata for pagination query parameters.
        /// </summary>
        /// <param name="operation">OpenAPI operation being generated.</param>
        /// <param name="context">Current operation transformer context.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public Task TransformAsync(
            OpenApiOperation operation,
            OpenApiOperationTransformerContext context,
            CancellationToken cancellationToken)
        {
            var methodInfo = ResolveMethodInfo(context.Description);

            PaginationOpenApiOperationDescriptionApplier.Apply(
                operation,
                methodInfo,
                context.Description.ActionDescriptor.EndpointMetadata);

            return Task.CompletedTask;
        }

        private static MethodInfo? ResolveMethodInfo(ApiDescription apiDescription)
        {
            if (apiDescription.ActionDescriptor is ControllerActionDescriptor controllerActionDescriptor)
            {
                return controllerActionDescriptor.MethodInfo;
            }

            return apiDescription.ActionDescriptor.EndpointMetadata?.OfType<MethodInfo>().FirstOrDefault();
        }
    }
}
