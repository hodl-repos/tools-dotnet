using System;
using Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.OpenApi;

namespace tools_dotnet.Pagination.OpenApi
{
    /// <summary>
    /// Enriches pagination query parameters in OpenAPI docs based on <see cref="Attributes.PaginationAttribute"/>.
    /// </summary>
    public class PaginationOpenApiOperationFilter : IOperationFilter
    {
        /// <summary>
        /// Updates operation metadata for pagination query parameters.
        /// </summary>
        /// <param name="operation">OpenAPI operation being generated.</param>
        /// <param name="context">Current operation filter context.</param>
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (operation == null)
            {
                throw new ArgumentNullException(nameof(operation));
            }

            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            PaginationOpenApiOperationDescriptionApplier.Apply(
                operation,
                context.MethodInfo,
                context.ApiDescription.ActionDescriptor.EndpointMetadata);
        }
    }
}
