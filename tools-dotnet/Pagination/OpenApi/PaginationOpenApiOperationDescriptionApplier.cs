using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi;
using tools_dotnet.Pagination.Services;
using tools_dotnet.Paging;

namespace tools_dotnet.Pagination.OpenApi
{
    internal static class PaginationOpenApiOperationDescriptionApplier
    {
        public static void Apply(
            OpenApiOperation operation,
            MethodInfo? methodInfo,
            IList<object>? endpointMetadata,
            IReadOnlyList<IPaginationCustomFilterMethods>? customFilterMethods = null,
            IReadOnlyList<IPaginationCustomSortsMethods>? customSortMethods = null
        )
        {
            if (operation == null)
            {
                throw new ArgumentNullException(nameof(operation));
            }

            var modelType = ResolveModelType(methodInfo, endpointMetadata);

            if (modelType == null)
            {
                return;
            }

            var fieldDescriptors = PaginationOpenApiMetadataProvider.GetFieldDescriptors(
                modelType,
                customFilterMethods,
                customSortMethods
            );

            if (fieldDescriptors.Count == 0)
            {
                return;
            }

            var filtersDescription = PaginationOpenApiDescriptionBuilder.BuildFiltersDescription(
                fieldDescriptors
            );
            var sortsDescription = PaginationOpenApiDescriptionBuilder.BuildSortsDescription(
                fieldDescriptors
            );

            if (operation.Parameters != null)
            {
                AppendQueryParameterDescription(
                    operation.Parameters,
                    "filters",
                    filtersDescription
                );
                AppendQueryParameterDescription(operation.Parameters, "sorts", sortsDescription);
            }
        }

        private static Type? ResolveModelType(
            MethodInfo? methodInfo,
            IList<object>? endpointMetadata
        )
        {
            var explicitType = ResolveModelTypeFromAttribute(methodInfo, endpointMetadata);

            if (explicitType != null)
            {
                return explicitType;
            }

            return ResolveModelTypeFromReturnType(methodInfo?.ReturnType);
        }

        private static Type? ResolveModelTypeFromAttribute(
            MethodInfo? methodInfo,
            IList<object>? endpointMetadata
        )
        {
            var methodAttribute = methodInfo?.GetCustomAttribute<PaginationOpenApiTypeAttribute>(
                inherit: true
            );

            if (methodAttribute != null)
            {
                return methodAttribute.ModelType;
            }

            var declaringTypeAttribute =
                methodInfo?.DeclaringType?.GetCustomAttribute<PaginationOpenApiTypeAttribute>(
                    inherit: true
                );

            if (declaringTypeAttribute != null)
            {
                return declaringTypeAttribute.ModelType;
            }

            var endpointMetadataAttribute = endpointMetadata
                ?.OfType<PaginationOpenApiTypeAttribute>()
                .FirstOrDefault();

            return endpointMetadataAttribute?.ModelType;
        }

        private static Type? ResolveModelTypeFromReturnType(Type? returnType)
        {
            if (returnType == null)
            {
                return null;
            }

            var unwrappedType = UnwrapTaskType(returnType);
            unwrappedType = UnwrapActionResultType(unwrappedType);

            return TryResolveGenericArgument(unwrappedType, typeof(IPagedList<>));
        }

        private static Type UnwrapTaskType(Type type)
        {
            if (!type.IsGenericType)
            {
                return type;
            }

            var genericTypeDefinition = type.GetGenericTypeDefinition();

            if (
                genericTypeDefinition == typeof(Task<>)
                || genericTypeDefinition == typeof(ValueTask<>)
            )
            {
                return type.GetGenericArguments()[0];
            }

            return type;
        }

        private static Type UnwrapActionResultType(Type type)
        {
            if (!type.IsGenericType)
            {
                return type;
            }

            var genericTypeDefinition = type.GetGenericTypeDefinition();

            if (genericTypeDefinition == typeof(ActionResult<>))
            {
                return type.GetGenericArguments()[0];
            }

            return type;
        }

        private static Type? TryResolveGenericArgument(Type type, Type genericTypeDefinition)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == genericTypeDefinition)
            {
                return type.GetGenericArguments()[0];
            }

            foreach (var implementedInterface in type.GetInterfaces())
            {
                if (
                    implementedInterface.IsGenericType
                    && implementedInterface.GetGenericTypeDefinition() == genericTypeDefinition
                )
                {
                    return implementedInterface.GetGenericArguments()[0];
                }
            }

            return null;
        }

        private static void AppendQueryParameterDescription(
            IList<IOpenApiParameter> parameters,
            string parameterName,
            string description
        )
        {
            if (string.IsNullOrWhiteSpace(description))
            {
                return;
            }

            var parameter = parameters.FirstOrDefault(x =>
                x.In == ParameterLocation.Query
                && string.Equals(x.Name, parameterName, StringComparison.OrdinalIgnoreCase)
            );

            if (parameter == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(parameter.Description))
            {
                parameter.Description = description;
                return;
            }

            if (!parameter.Description.Contains(description, StringComparison.Ordinal))
            {
                parameter.Description =
                    $"{parameter.Description}{Environment.NewLine}{Environment.NewLine}{description}";
            }
        }
    }
}

