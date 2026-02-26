using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using tools_dotnet.Pagination.Services;

namespace tools_dotnet.Pagination.OpenApi
{
    /// <summary>
    /// Enriches pagination query parameters in OpenAPI docs based on <see cref="Attributes.PaginationAttribute"/>.
    /// </summary>
    public sealed class PaginationOpenApiOperationTransformer : IOpenApiOperationTransformer
    {
        private readonly IReadOnlyList<IPaginationCustomFilterMethods> _customFilterMethods;
        private readonly IReadOnlyList<IPaginationCustomSortsMethods> _customSortMethods;

        /// <summary>
        /// Creates a Microsoft.AspNetCore.OpenApi operation transformer for pagination metadata.
        /// </summary>
        /// <param name="customFilterMethods">Optional custom filter method containers used for OpenAPI metadata enrichment.</param>
        /// <param name="customSortMethods">Optional custom sort method containers used for OpenAPI metadata enrichment.</param>
        public PaginationOpenApiOperationTransformer(
            IEnumerable<IPaginationCustomFilterMethods>? customFilterMethods = null,
            IEnumerable<IPaginationCustomSortsMethods>? customSortMethods = null)
        {
            _customFilterMethods = customFilterMethods?.ToArray() ?? Array.Empty<IPaginationCustomFilterMethods>();
            _customSortMethods = customSortMethods?.ToArray() ?? Array.Empty<IPaginationCustomSortsMethods>();
        }

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
                context.Description.ActionDescriptor.EndpointMetadata,
                _customFilterMethods,
                _customSortMethods);

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
