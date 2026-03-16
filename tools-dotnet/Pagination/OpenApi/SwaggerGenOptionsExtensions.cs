using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Swashbuckle.AspNetCore.SwaggerGen;
using tools_dotnet.Pagination.Services;

namespace tools_dotnet.Pagination.OpenApi
{
    /// <summary>
    /// Swagger configuration helpers for pagination metadata.
    /// </summary>
    public static class SwaggerGenOptionsExtensions
    {
        /// <summary>
        /// Registers pagination query metadata generation for Swagger operations.
        /// </summary>
        /// <param name="options">Swagger generation options.</param>
        /// <param name="customFilterMethods">Optional custom filter method containers to include in generated metadata.</param>
        /// <param name="customSortMethods">Optional custom sort method containers to include in generated metadata.</param>
        /// <returns>The same <paramref name="options"/> instance for chaining.</returns>
        public static SwaggerGenOptions AddPaginationOpenApiSupport(
            this SwaggerGenOptions options,
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
                options.OperationFilter<PaginationOpenApiOperationFilter>();
                return options;
            }

            options.OperationFilter<PaginationOpenApiOperationFilter>(
                customFilterMethods?.ToArray() ?? Array.Empty<IPaginationCustomFilterMethods>(),
                customSortMethods?.ToArray() ?? Array.Empty<IPaginationCustomSortsMethods>()
            );
            return options;
        }
    }
}

