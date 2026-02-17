using tools_dotnet.Pagination.Attributes;
using tools_dotnet.Pagination.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace tools_dotnet.Pagination.OpenApi
{
    internal static class PaginationOpenApiMetadataProvider
    {
        private static readonly IReadOnlyList<PaginationOperator> EqualityOperators =
            new[] { PaginationOperator.Equal, PaginationOperator.NotEquals };

        private static readonly IReadOnlyList<PaginationOperator> ComparableOperators =
            new[]
            {
                PaginationOperator.Equal,
                PaginationOperator.NotEquals,
                PaginationOperator.GreaterThan,
                PaginationOperator.GreaterThanOrEqual,
                PaginationOperator.LessThan,
                PaginationOperator.LessThanOrEqual
            };

        private static readonly IReadOnlyList<PaginationOperator> StringOperators =
            new[]
            {
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
            };

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

        public static IReadOnlyList<PaginationOpenApiFieldDescriptor> GetFieldDescriptors(Type modelType)
        {
            if (modelType == null)
            {
                throw new ArgumentNullException(nameof(modelType));
            }

            var result = new List<PaginationOpenApiFieldDescriptor>();
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
                var operators = attribute.CanFilter ? ResolveFilterOperators(memberType) : Array.Empty<PaginationOperator>();

                result.Add(new PaginationOpenApiFieldDescriptor(
                    memberName,
                    memberType,
                    attribute.CanFilter,
                    attribute.CanSort,
                    operators));
            }

            return result
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
    }
}
