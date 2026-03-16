using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using tools_dotnet.Pagination.Services;

namespace tools_dotnet.Pagination.OpenApi
{
    /// <summary>
    /// Enriches pagination query parameters in OpenAPI docs based on <see cref="Attributes.PaginationAttribute"/>.
    /// </summary>
    public class PaginationOpenApiOperationFilter : IOperationFilter
    {
        private readonly IReadOnlyList<IPaginationCustomFilterMethods> _customFilterMethods;
        private readonly IReadOnlyList<IPaginationCustomSortsMethods> _customSortMethods;

        /// <summary>
        /// Creates a Swagger operation filter for pagination metadata.
        /// </summary>
        /// <param name="customFilterMethods">Optional custom filter method containers used for OpenAPI metadata enrichment.</param>
        /// <param name="customSortMethods">Optional custom sort method containers used for OpenAPI metadata enrichment.</param>
        public PaginationOpenApiOperationFilter(
            IEnumerable<IPaginationCustomFilterMethods>? customFilterMethods = null,
            IEnumerable<IPaginationCustomSortsMethods>? customSortMethods = null
        )
        {
            _customFilterMethods =
                customFilterMethods?.ToArray() ?? Array.Empty<IPaginationCustomFilterMethods>();
            _customSortMethods =
                customSortMethods?.ToArray() ?? Array.Empty<IPaginationCustomSortsMethods>();
        }

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
                context.ApiDescription.ActionDescriptor.EndpointMetadata,
                _customFilterMethods,
                _customSortMethods
            );
        }
    }
}

