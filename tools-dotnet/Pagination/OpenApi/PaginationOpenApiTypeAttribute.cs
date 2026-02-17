using System;

namespace tools_dotnet.Pagination.OpenApi
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true)]
    public sealed class PaginationOpenApiTypeAttribute : Attribute
    {
        public PaginationOpenApiTypeAttribute(Type modelType)
        {
            ModelType = modelType ?? throw new ArgumentNullException(nameof(modelType));
        }

        public Type ModelType { get; }
    }
}
