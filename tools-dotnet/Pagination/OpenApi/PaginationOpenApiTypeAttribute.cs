using System;

namespace tools_dotnet.Pagination.OpenApi
{
    /// <summary>
    /// Specifies the model type used to generate pagination OpenAPI metadata.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true)]
    public sealed class PaginationOpenApiTypeAttribute : Attribute
    {
        /// <summary>
        /// Creates the attribute with a target model type.
        /// </summary>
        /// <param name="modelType">Type containing <see cref="Attributes.PaginationAttribute"/> metadata.</param>
        public PaginationOpenApiTypeAttribute(Type modelType)
        {
            ModelType = modelType ?? throw new ArgumentNullException(nameof(modelType));
        }

        /// <summary>
        /// Gets the model type used for pagination OpenAPI metadata.
        /// </summary>
        public Type ModelType { get; }
    }
}
