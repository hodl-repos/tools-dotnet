using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.OpenApi;
using tools_dotnet.Pagination.Services;

namespace tools_dotnet.Pagination.OpenApi
{
    /// <summary>
    /// Microsoft.AspNetCore.OpenApi configuration helpers for pagination metadata.
    /// </summary>
    public static class OpenApiOptionsExtensions
    {
        /// <summary>
        /// Registers pagination query metadata generation for OpenAPI operations.
        /// </summary>
        /// <param name="options">OpenAPI generation options.</param>
        /// <param name="customFilterMethods">Optional custom filter method containers to include in generated metadata.</param>
        /// <param name="customSortMethods">Optional custom sort method containers to include in generated metadata.</param>
        /// <returns>The same <paramref name="options"/> instance for chaining.</returns>
        public static OpenApiOptions AddPaginationOpenApiSupport(
            this OpenApiOptions options,
            IEnumerable<IPaginationCustomFilterMethods>? customFilterMethods = null,
            IEnumerable<IPaginationCustomSortsMethods>? customSortMethods = null
        )
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (customFilterMethods == null && customSortMethods == null)
            {
                options.AddOperationTransformer<PaginationOpenApiOperationTransformer>();
                return options;
            }

            options.AddOperationTransformer(
                new PaginationOpenApiOperationTransformer(customFilterMethods, customSortMethods)
            );
            return options;
        }
    }
}

