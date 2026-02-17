using Microsoft.Extensions.DependencyInjection;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;

namespace tools_dotnet.Pagination.OpenApi
{
    public static class SwaggerGenOptionsExtensions
    {
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
