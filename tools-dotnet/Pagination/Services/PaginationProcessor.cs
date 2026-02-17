using tools_dotnet.Pagination.Models;
using tools_dotnet.Pagination.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace tools_dotnet.Pagination.Services
{
    /// <summary>
    /// Applies filtering, sorting, and pagination to an <see cref="IQueryable{T}"/>.
    /// </summary>
    public class PaginationProcessor : IPaginationProcessor
    {
        private static readonly MethodInfo OrderByMethod = GetOrderMethod(nameof(Queryable.OrderBy));
        private static readonly MethodInfo OrderByDescendingMethod = GetOrderMethod(nameof(Queryable.OrderByDescending));
        private static readonly MethodInfo ThenByMethod = GetOrderMethod(nameof(Queryable.ThenBy));
        private static readonly MethodInfo ThenByDescendingMethod = GetOrderMethod(nameof(Queryable.ThenByDescending));

        private readonly IPaginationModelDeserializer _deserializer;
        private readonly IReadOnlyList<IPaginationFilterExpressionProvider> _filterExpressionProviders;

        /// <summary>
        /// Creates a pagination processor.
        /// </summary>
        /// <param name="deserializer">Optional deserializer for incoming models.</param>
        /// <param name="filterExpressionProviders">Optional filter providers. If not supplied, the default provider is used.</param>
        public PaginationProcessor(
            IPaginationModelDeserializer? deserializer = null,
            IEnumerable<IPaginationFilterExpressionProvider>? filterExpressionProviders = null)
        {
            _deserializer = deserializer ?? new PaginationModelDeserializer();

            var providers = filterExpressionProviders?.ToList() ?? new List<IPaginationFilterExpressionProvider>();

            if (providers.Count == 0)
            {
                providers.Add(new DefaultPaginationFilterExpressionProvider());
            }

            _filterExpressionProviders = providers;
        }

        /// <inheritdoc />
        public IQueryable<TEntity> Apply<TEntity>(
            PaginationModel model,
            IQueryable<TEntity> source,
            object[]? dataForCustomMethods = null,
            bool applyFiltering = true,
            bool applySorting = true,
            bool applyPagination = true)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var deserializedModel = _deserializer.Deserialize(model);
            var query = source;

            if (applyFiltering)
            {
                query = ApplyFilters(query, deserializedModel.Filters, dataForCustomMethods);
            }

            if (applySorting)
            {
                query = ApplySorts(query, deserializedModel.Sorts);
            }

            if (applyPagination)
            {
                query = ApplyPagination(query, deserializedModel.Page, deserializedModel.PageSize);
            }

            return query;
        }

        private IQueryable<TEntity> ApplyFilters<TEntity>(
            IQueryable<TEntity> query,
            IReadOnlyList<PaginationFilterTerm> filters,
            object[]? dataForCustomMethods)
        {
            if (filters.Count == 0)
            {
                return query;
            }

            foreach (var filter in filters)
            {
                var parameterExpression = Expression.Parameter(typeof(TEntity), "entity");
                Expression? filterExpression = null;

                foreach (var field in filter.Fields)
                {
                    if (!TryBuildMemberExpression(parameterExpression, field, out var memberExpression) || memberExpression == null)
                    {
                        continue;
                    }

                    var typedValues = ConvertValues(filter.Values, memberExpression.Type);

                    if (typedValues.Count == 0)
                    {
                        continue;
                    }

                    var context = new PaginationFilterExpressionContext(
                        parameterExpression,
                        memberExpression,
                        filter,
                        typedValues,
                        dataForCustomMethods);

                    var memberFilterExpression = BuildFilterExpression(context);

                    if (memberFilterExpression == null)
                    {
                        continue;
                    }

                    filterExpression = filterExpression == null
                        ? memberFilterExpression
                        : Expression.OrElse(filterExpression, memberFilterExpression);
                }

                if (filterExpression != null)
                {
                    var lambda = Expression.Lambda<Func<TEntity, bool>>(filterExpression, parameterExpression);
                    query = query.Where(lambda);
                }
            }

            return query;
        }

        private Expression? BuildFilterExpression(PaginationFilterExpressionContext context)
        {
            foreach (var expressionProvider in _filterExpressionProviders)
            {
                if (expressionProvider.TryBuildExpression(context, out var expression) && expression != null)
                {
                    return expression;
                }
            }

            return null;
        }

        private static IQueryable<TEntity> ApplySorts<TEntity>(IQueryable<TEntity> query, IReadOnlyList<PaginationSortTerm> sorts)
        {
            if (sorts.Count == 0)
            {
                return query;
            }

            IOrderedQueryable<TEntity>? orderedQuery = null;

            foreach (var sort in sorts)
            {
                var parameterExpression = Expression.Parameter(typeof(TEntity), "entity");

                if (!TryBuildMemberExpression(parameterExpression, sort.Field, out var memberExpression, applyFilterRules: false) || memberExpression == null)
                {
                    continue;
                }

                var keySelector = Expression.Lambda(memberExpression, parameterExpression);

                if (orderedQuery == null)
                {
                    orderedQuery = (IOrderedQueryable<TEntity>)InvokeOrderMethod(
                        sort.Descending ? OrderByDescendingMethod : OrderByMethod,
                        typeof(TEntity),
                        query,
                        keySelector);
                }
                else
                {
                    orderedQuery = (IOrderedQueryable<TEntity>)InvokeOrderMethod(
                        sort.Descending ? ThenByDescendingMethod : ThenByMethod,
                        typeof(TEntity),
                        orderedQuery,
                        keySelector);
                }
            }

            return orderedQuery ?? query;
        }

        private static IQueryable<TEntity> ApplyPagination<TEntity>(IQueryable<TEntity> query, int page, int pageSize)
        {
            var skip = (page - 1) * pageSize;
            return query.Skip(skip).Take(pageSize);
        }

        private static IReadOnlyList<object?> ConvertValues(IReadOnlyList<string> rawValues, Type targetType)
        {
            var values = new List<object?>();

            foreach (var rawValue in rawValues)
            {
                if (TryConvertValue(rawValue, targetType, out var converted))
                {
                    values.Add(converted);
                }
            }

            return values;
        }

        private static bool TryConvertValue(string rawValue, Type targetType, out object? convertedValue)
        {
            var nullableType = Nullable.GetUnderlyingType(targetType);
            var conversionType = nullableType ?? targetType;

            if (string.Equals(rawValue, "\\null", StringComparison.OrdinalIgnoreCase))
            {
                if (conversionType == typeof(string))
                {
                    convertedValue = "null";
                    return true;
                }
            }

            if (string.Equals(rawValue, "null", StringComparison.OrdinalIgnoreCase))
            {
                if (nullableType == null && conversionType.IsValueType)
                {
                    convertedValue = null;
                    return false;
                }

                convertedValue = null;
                return true;
            }

            if (conversionType == typeof(string))
            {
                convertedValue = rawValue;
                return true;
            }

            if (conversionType == typeof(Guid))
            {
                var parsed = Guid.TryParse(rawValue, out var guidValue);
                convertedValue = parsed ? guidValue : null;
                return parsed;
            }

            if (conversionType == typeof(DateTimeOffset))
            {
                var parsed = DateTimeOffset.TryParse(rawValue, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dateTimeOffsetValue);
                convertedValue = parsed ? dateTimeOffsetValue : null;
                return parsed;
            }

            if (conversionType == typeof(DateTime))
            {
                var parsed = DateTime.TryParse(rawValue, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dateTimeValue);
                convertedValue = parsed ? dateTimeValue : null;
                return parsed;
            }

            if (conversionType == typeof(bool))
            {
                var parsed = bool.TryParse(rawValue, out var boolValue);
                convertedValue = parsed ? boolValue : null;
                return parsed;
            }

            if (conversionType.IsEnum)
            {
                try
                {
                    convertedValue = System.Enum.Parse(conversionType, rawValue, ignoreCase: true);
                    return true;
                }
                catch (ArgumentException)
                {
                    convertedValue = null;
                    return false;
                }
            }

            try
            {
                convertedValue = Convert.ChangeType(rawValue, conversionType, CultureInfo.InvariantCulture);
                return true;
            }
            catch (InvalidCastException)
            {
                var converter = TypeDescriptor.GetConverter(conversionType);

                if (converter.CanConvertFrom(typeof(string)))
                {
                    try
                    {
                        convertedValue = converter.ConvertFromInvariantString(rawValue);
                        return true;
                    }
                    catch (FormatException)
                    {
                    }
                    catch (NotSupportedException)
                    {
                    }
                }

                convertedValue = null;
                return false;
            }
            catch (FormatException)
            {
                convertedValue = null;
                return false;
            }
            catch (OverflowException)
            {
                convertedValue = null;
                return false;
            }
        }

        private static bool TryBuildMemberExpression(
            Expression source,
            string fieldPath,
            out Expression? memberExpression,
            bool applyFilterRules = true)
        {
            memberExpression = source;

            var fieldSegments = fieldPath
                .Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (fieldSegments.Length == 0)
            {
                memberExpression = null;
                return false;
            }

            foreach (var segment in fieldSegments)
            {
                var property = memberExpression.Type
                    .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .FirstOrDefault(x => MatchesSegment(x, segment, applyFilterRules));

                if (property != null)
                {
                    memberExpression = Expression.Property(memberExpression, property);
                    continue;
                }

                var field = memberExpression.Type
                    .GetFields(BindingFlags.Public | BindingFlags.Instance)
                    .FirstOrDefault(x => MatchesSegment(x, segment, applyFilterRules));

                if (field != null)
                {
                    memberExpression = Expression.Field(memberExpression, field);
                    continue;
                }

                memberExpression = null;
                return false;
            }

            return true;
        }

        private static bool MatchesSegment(MemberInfo memberInfo, string segment, bool applyFilterRules)
        {
            var attribute = memberInfo.GetCustomAttribute<PaginationAttribute>();
            var memberNameMatches = string.Equals(memberInfo.Name, segment, StringComparison.OrdinalIgnoreCase);
            var attributeNameMatches = attribute != null &&
                                       !string.IsNullOrWhiteSpace(attribute.Name) &&
                                       string.Equals(attribute.Name, segment, StringComparison.OrdinalIgnoreCase);

            if (!memberNameMatches && !attributeNameMatches)
            {
                return false;
            }

            if (attribute == null)
            {
                return true;
            }

            return applyFilterRules ? attribute.CanFilter : attribute.CanSort;
        }

        private static object InvokeOrderMethod(MethodInfo orderMethod, Type entityType, object source, LambdaExpression keySelector)
        {
            var closedMethod = orderMethod.MakeGenericMethod(entityType, keySelector.ReturnType);
            return closedMethod.Invoke(null, new object[] { source, keySelector })!;
        }

        private static MethodInfo GetOrderMethod(string methodName)
        {
            return typeof(Queryable)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Single(method =>
                    method.Name == methodName &&
                    method.IsGenericMethodDefinition &&
                    method.GetParameters().Length == 2);
        }
    }
}
