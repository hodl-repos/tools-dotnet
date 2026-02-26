using tools_dotnet.Pagination.Attributes;
using tools_dotnet.Pagination.Models;
using tools_dotnet.Pagination.Services;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace tools_dotnet.Pagination.OpenApi
{
    internal static class PaginationOpenApiMetadataProvider
    {
        private const int MaxNestedDepth = 8;

        private static readonly IReadOnlyList<PaginationOperator> EqualityOperators =
            [PaginationOperator.Equal, PaginationOperator.NotEquals];

        private static readonly IReadOnlyList<PaginationOperator> ComparableOperators =
            [
                PaginationOperator.Equal,
                PaginationOperator.NotEquals,
                PaginationOperator.GreaterThan,
                PaginationOperator.GreaterThanOrEqual,
                PaginationOperator.LessThan,
                PaginationOperator.LessThanOrEqual
            ];

        private static readonly IReadOnlyList<PaginationOperator> StringOperators =
            [
                PaginationOperator.Equal,
                PaginationOperator.EqualCaseInsensitive,
                PaginationOperator.NotEquals,
                PaginationOperator.NotEqualsCaseInsensitive,
                PaginationOperator.Contains,
                PaginationOperator.ContainsCaseInsensitive,
                PaginationOperator.NotContains,
                PaginationOperator.NotContainsCaseInsensitive,
                PaginationOperator.StartsWith,
                PaginationOperator.StartsWithCaseInsensitive,
                PaginationOperator.NotStartsWith,
                PaginationOperator.NotStartsWithCaseInsensitive,
                PaginationOperator.EndsWith,
                PaginationOperator.EndsWithCaseInsensitive,
                PaginationOperator.NotEndsWith,
                PaginationOperator.NotEndsWithCaseInsensitive
            ];

        private static readonly HashSet<Type> TypesWithComparisonOperators = new()
        {
            typeof(byte),
            typeof(sbyte),
            typeof(short),
            typeof(ushort),
            typeof(int),
            typeof(uint),
            typeof(long),
            typeof(ulong),
            typeof(float),
            typeof(double),
            typeof(decimal),
            typeof(char),
            typeof(DateTime),
            typeof(DateTimeOffset),
            typeof(DateOnly),
            typeof(TimeOnly),
            typeof(TimeSpan)
        };

        public static IReadOnlyList<PaginationOpenApiFieldDescriptor> GetFieldDescriptors(
            Type modelType,
            IEnumerable<IPaginationCustomFilterMethods>? customFilterMethods = null,
            IEnumerable<IPaginationCustomSortsMethods>? customSortMethods = null)
        {
            if (modelType == null)
            {
                throw new ArgumentNullException(nameof(modelType));
            }

            var result = new List<PaginationOpenApiFieldDescriptor>();
            var activePathTypes = new HashSet<Type>();

            CollectDescriptors(
                modelType,
                prefix: null,
                depth: 0,
                result,
                activePathTypes);

            if (customFilterMethods != null)
            {
                CollectCustomFilterMethodDescriptors(modelType, customFilterMethods, result);
            }

            if (customSortMethods != null)
            {
                CollectCustomSortMethodDescriptors(modelType, customSortMethods, result);
            }

            return result
                .GroupBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                .Select(x => x.First())
                .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        public static string GetTypeDisplayName(Type memberType)
        {
            var underlyingType = Nullable.GetUnderlyingType(memberType) ?? memberType;
            var suffix = Nullable.GetUnderlyingType(memberType) != null ? "?" : string.Empty;

            if (underlyingType == typeof(string))
            {
                return $"string{suffix}";
            }

            if (underlyingType == typeof(bool))
            {
                return $"bool{suffix}";
            }

            if (underlyingType == typeof(Guid))
            {
                return $"guid{suffix}";
            }

            if (underlyingType.IsEnum)
            {
                return $"enum({underlyingType.Name}){suffix}";
            }

            if (TypesWithComparisonOperators.Contains(underlyingType))
            {
                if (underlyingType == typeof(DateTime) || underlyingType == typeof(DateTimeOffset) || underlyingType == typeof(DateOnly))
                {
                    return $"date{suffix}";
                }

                if (underlyingType == typeof(TimeOnly) || underlyingType == typeof(TimeSpan))
                {
                    return $"time{suffix}";
                }

                return $"number{suffix}";
            }

            return $"{underlyingType.Name}{suffix}";
        }

        private static IReadOnlyList<PaginationOperator> ResolveFilterOperators(Type memberType)
        {
            var underlyingType = Nullable.GetUnderlyingType(memberType) ?? memberType;

            if (underlyingType == typeof(string))
            {
                return StringOperators;
            }

            if (underlyingType == typeof(bool) || underlyingType == typeof(Guid) || underlyingType.IsEnum)
            {
                return EqualityOperators;
            }

            if (TypesWithComparisonOperators.Contains(underlyingType))
            {
                return ComparableOperators;
            }

            return EqualityOperators;
        }

        private static void CollectDescriptors(
            Type modelType,
            string? prefix,
            int depth,
            ICollection<PaginationOpenApiFieldDescriptor> result,
            ISet<Type> activePathTypes)
        {
            if (depth > MaxNestedDepth)
            {
                return;
            }

            if (!activePathTypes.Add(modelType))
            {
                return;
            }

            try
            {
                var members = modelType
                    .GetMembers(BindingFlags.Public | BindingFlags.Instance)
                    .Where(x => x.MemberType == MemberTypes.Property || x.MemberType == MemberTypes.Field);

                foreach (var member in members)
                {
                    var attribute = member.GetCustomAttribute<PaginationAttribute>();

                    if (attribute == null)
                    {
                        continue;
                    }

                    var memberType = member switch
                    {
                        PropertyInfo propertyInfo => propertyInfo.PropertyType,
                        FieldInfo fieldInfo => fieldInfo.FieldType,
                        _ => null
                    };

                    if (memberType == null)
                    {
                        continue;
                    }

                    var memberName = string.IsNullOrWhiteSpace(attribute.Name) ? member.Name : attribute.Name!;
                    var fieldPath = string.IsNullOrWhiteSpace(prefix) ? memberName : $"{prefix}.{memberName}";
                    var operators = attribute.CanFilter ? ResolveFilterOperators(memberType) : Array.Empty<PaginationOperator>();

                    result.Add(new PaginationOpenApiFieldDescriptor(
                        fieldPath,
                        memberType,
                        attribute.CanFilter,
                        attribute.CanSort,
                        operators));

                    if (!attribute.CanFilterSubProperties && !attribute.CanSortSubProperties)
                    {
                        continue;
                    }

                    if (!CanTraverseNestedMembers(memberType))
                    {
                        continue;
                    }

                    var nestedType = Nullable.GetUnderlyingType(memberType) ?? memberType;

                    CollectDescriptors(
                        nestedType,
                        fieldPath,
                        depth + 1,
                        result,
                        activePathTypes);
                }
            }
            finally
            {
                activePathTypes.Remove(modelType);
            }
        }

        private static void CollectCustomFilterMethodDescriptors(
            Type modelType,
            IEnumerable<IPaginationCustomFilterMethods> customFilterMethods,
            ICollection<PaginationOpenApiFieldDescriptor> result)
        {
            foreach (var methodsContainer in customFilterMethods)
            {
                foreach (var method in methodsContainer
                    .GetType()
                    .GetMethods(BindingFlags.Instance | BindingFlags.Public)
                    .Where(x => !x.IsSpecialName && x.DeclaringType != typeof(object)))
                {
                    if (!TryCloseCustomMethod(method, modelType, out var closedMethod) || closedMethod == null)
                    {
                        continue;
                    }

                    if (!IsValidCustomFilterMethodSignature(closedMethod, modelType))
                    {
                        continue;
                    }

                    result.Add(new PaginationOpenApiFieldDescriptor(
                        method.Name,
                        typeof(string),
                        canFilter: true,
                        canSort: false,
                        operators: Array.Empty<PaginationOperator>(),
                        filterTypeDisplayNameOverride: "custom"));
                }
            }
        }

        private static void CollectCustomSortMethodDescriptors(
            Type modelType,
            IEnumerable<IPaginationCustomSortsMethods> customSortMethods,
            ICollection<PaginationOpenApiFieldDescriptor> result)
        {
            foreach (var methodsContainer in customSortMethods)
            {
                foreach (var method in methodsContainer
                    .GetType()
                    .GetMethods(BindingFlags.Instance | BindingFlags.Public)
                    .Where(x => !x.IsSpecialName && x.DeclaringType != typeof(object)))
                {
                    if (!TryCloseCustomMethod(method, modelType, out var closedMethod) || closedMethod == null)
                    {
                        continue;
                    }

                    if (!IsValidCustomSortMethodSignature(closedMethod, modelType))
                    {
                        continue;
                    }

                    result.Add(new PaginationOpenApiFieldDescriptor(
                        method.Name,
                        typeof(string),
                        canFilter: false,
                        canSort: true,
                        operators: Array.Empty<PaginationOperator>()));
                }
            }
        }

        private static bool TryCloseCustomMethod(MethodInfo method, Type modelType, out MethodInfo? closedMethod)
        {
            if (!method.IsGenericMethodDefinition)
            {
                closedMethod = method;
                return true;
            }

            var genericArguments = method.GetGenericArguments();

            if (genericArguments.Length != 1)
            {
                closedMethod = null;
                return false;
            }

            try
            {
                closedMethod = method.MakeGenericMethod(modelType);
                return true;
            }
            catch (ArgumentException)
            {
                closedMethod = null;
                return false;
            }
        }

        private static bool IsValidCustomFilterMethodSignature(MethodInfo method, Type modelType)
        {
            var parameters = method.GetParameters();
            var expectedQueryableType = typeof(IQueryable<>).MakeGenericType(modelType);

            if (parameters.Length == 0 || parameters.Length > 4)
            {
                return false;
            }

            if (!parameters[0].ParameterType.IsAssignableFrom(expectedQueryableType))
            {
                return false;
            }

            if (parameters.Length >= 2 && parameters[1].ParameterType != typeof(string))
            {
                return false;
            }

            if (parameters.Length >= 3 &&
                parameters[2].ParameterType != typeof(string[]) &&
                parameters[2].ParameterType != typeof(IReadOnlyList<string>) &&
                parameters[2].ParameterType != typeof(IEnumerable<string>))
            {
                return false;
            }

            if (parameters.Length == 4 && parameters[3].ParameterType != typeof(object[]))
            {
                return false;
            }

            return typeof(IQueryable<>).MakeGenericType(modelType).IsAssignableFrom(method.ReturnType);
        }

        private static bool IsValidCustomSortMethodSignature(MethodInfo method, Type modelType)
        {
            var parameters = method.GetParameters();
            var expectedQueryableType = typeof(IQueryable<>).MakeGenericType(modelType);

            if (parameters.Length == 0 || parameters.Length > 4)
            {
                return false;
            }

            if (!parameters[0].ParameterType.IsAssignableFrom(expectedQueryableType))
            {
                return false;
            }

            if (parameters.Length >= 2 && parameters[1].ParameterType != typeof(bool))
            {
                return false;
            }

            if (parameters.Length >= 3 && parameters[2].ParameterType != typeof(bool))
            {
                return false;
            }

            if (parameters.Length == 4 && parameters[3].ParameterType != typeof(object[]))
            {
                return false;
            }

            return typeof(IQueryable<>).MakeGenericType(modelType).IsAssignableFrom(method.ReturnType);
        }

        private static bool CanTraverseNestedMembers(Type memberType)
        {
            var underlyingType = Nullable.GetUnderlyingType(memberType) ?? memberType;

            if (underlyingType == typeof(string) ||
                underlyingType == typeof(Guid) ||
                underlyingType == typeof(decimal) ||
                underlyingType == typeof(DateTime) ||
                underlyingType == typeof(DateTimeOffset) ||
                underlyingType == typeof(DateOnly) ||
                underlyingType == typeof(TimeOnly) ||
                underlyingType == typeof(TimeSpan) ||
                underlyingType.IsEnum ||
                underlyingType.IsPrimitive)
            {
                return false;
            }

            if (underlyingType != typeof(byte[]) && typeof(IEnumerable).IsAssignableFrom(underlyingType))
            {
                return false;
            }

            return true;
        }
    }
}
