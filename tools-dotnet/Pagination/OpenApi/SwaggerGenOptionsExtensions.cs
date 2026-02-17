using Microsoft.Extensions.DependencyInjection;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;

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
        /// <returns>The same <paramref name="options"/> instance for chaining.</returns>
        public static SwaggerGenOptions AddPaginationOpenApiSupport(this SwaggerGenOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            options.OperationFilter<PaginationOpenApiOperationFilter>();
            return options;
        }
    }
}
