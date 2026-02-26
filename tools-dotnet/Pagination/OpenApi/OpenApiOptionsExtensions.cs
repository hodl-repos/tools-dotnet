using Microsoft.AspNetCore.OpenApi;
using System;

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
        /// <returns>The same <paramref name="options"/> instance for chaining.</returns>
        public static OpenApiOptions AddPaginationOpenApiSupport(this OpenApiOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            options.AddOperationTransformer<PaginationOpenApiOperationTransformer>();
            return options;
        }
    }
}
