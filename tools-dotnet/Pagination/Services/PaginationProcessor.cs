using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using tools_dotnet.Pagination.Attributes;
using tools_dotnet.Pagination.Models;

namespace tools_dotnet.Pagination.Services
{
    /// <summary>
    /// Applies filtering, sorting, and pagination to an <see cref="IQueryable{T}"/>.
    /// </summary>
    public class PaginationProcessor : IPaginationProcessor
    {
        private static readonly MethodInfo OrderByMethod = GetOrderMethod(
            nameof(Queryable.OrderBy)
        );
        private static readonly MethodInfo OrderByDescendingMethod = GetOrderMethod(
            nameof(Queryable.OrderByDescending)
        );
        private static readonly MethodInfo ThenByMethod = GetOrderMethod(nameof(Queryable.ThenBy));
        private static readonly MethodInfo ThenByDescendingMethod = GetOrderMethod(
            nameof(Queryable.ThenByDescending)
        );

        private readonly IPaginationModelDeserializer _deserializer;
        private readonly IReadOnlyList<IPaginationFilterExpressionProvider> _filterExpressionProviders;
        private readonly IReadOnlyList<IPaginationCustomFilterMethods> _customFilterMethods;
        private readonly IReadOnlyList<IPaginationCustomSortsMethods> _customSortMethods;

        /// <summary>
        /// Creates a pagination processor.
        /// </summary>
        /// <param name="deserializer">Optional deserializer for incoming models.</param>
        /// <param name="filterExpressionProviders">Optional filter providers. If not supplied, the default provider is used.</param>
        /// <param name="customFilterMethods">Optional custom filter method containers used when a field does not map to a member.</param>
        /// <param name="customSortMethods">Optional custom sort method containers used when a field does not map to a member.</param>
        public PaginationProcessor(
            IPaginationModelDeserializer? deserializer = null,
            IEnumerable<IPaginationFilterExpressionProvider>? filterExpressionProviders = null,
            IEnumerable<IPaginationCustomFilterMethods>? customFilterMethods = null,
            IEnumerable<IPaginationCustomSortsMethods>? customSortMethods = null
        )
        {
            _deserializer = deserializer ?? new PaginationModelDeserializer();

            var providers =
                filterExpressionProviders?.ToList()
                ?? new List<IPaginationFilterExpressionProvider>();

            if (providers.Count == 0)
            {
                providers.Add(new DefaultPaginationFilterExpressionProvider());
            }

            _filterExpressionProviders = providers;
            _customFilterMethods =
                customFilterMethods == null
                    ? Array.Empty<IPaginationCustomFilterMethods>()
                    : customFilterMethods.ToList();
            _customSortMethods =
                customSortMethods == null
                    ? Array.Empty<IPaginationCustomSortsMethods>()
                    : customSortMethods.ToList();
        }

        /// <inheritdoc />
        public IQueryable<TEntity> Apply<TEntity>(
            PaginationModel model,
            IQueryable<TEntity> source,
            object[]? dataForCustomMethods = null,
            bool applyFiltering = true,
            bool applySorting = true,
            bool applyPagination = true
        )
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
                query = ApplySorts(query, deserializedModel.Sorts, dataForCustomMethods);
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
            object[]? dataForCustomMethods
        )
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
                    if (
                        !TryBuildMemberExpression(
                            parameterExpression,
                            field,
                            out var memberExpression
                        )
                        || memberExpression == null
                    )
                    {
                        if (
                            TryApplyCustomFilterMethod(
                                query,
                                field,
                                filter,
                                dataForCustomMethods,
                                out var customMethodQuery
                            )
                        )
                        {
                            query = customMethodQuery;
                        }

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
                        dataForCustomMethods
                    );

                    var memberFilterExpression = BuildFilterExpression(context);

                    if (memberFilterExpression == null)
                    {
                        continue;
                    }

                    filterExpression =
                        filterExpression == null
                            ? memberFilterExpression
                            : Expression.OrElse(filterExpression, memberFilterExpression);
                }

                if (filterExpression != null)
                {
                    var lambda = Expression.Lambda<Func<TEntity, bool>>(
                        filterExpression,
                        parameterExpression
                    );
                    query = query.Where(lambda);
                }
            }

            return query;
        }

        private bool TryApplyCustomFilterMethod<TEntity>(
            IQueryable<TEntity> query,
            string field,
            PaginationFilterTerm filterTerm,
            object[]? dataForCustomMethods,
            out IQueryable<TEntity> filteredQuery
        )
        {
            filteredQuery = query;

            if (_customFilterMethods.Count == 0)
            {
                return false;
            }

            foreach (var customFilterMethods in _customFilterMethods)
            {
                if (
                    TryInvokeCustomFilterMethod(
                        query,
                        customFilterMethods,
                        field,
                        filterTerm,
                        dataForCustomMethods,
                        out filteredQuery
                    )
                )
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryInvokeCustomFilterMethod<TEntity>(
            IQueryable<TEntity> query,
            IPaginationCustomFilterMethods customFilterMethods,
            string methodName,
            PaginationFilterTerm filterTerm,
            object[]? dataForCustomMethods,
            out IQueryable<TEntity> filteredQuery
        )
        {
            filteredQuery = query;

            var methods = customFilterMethods
                .GetType()
                .GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .Where(x => string.Equals(x.Name, methodName, StringComparison.OrdinalIgnoreCase));

            foreach (var method in methods)
            {
                if (
                    !TryCloseCustomMethod<TEntity>(method, out var closedMethod)
                    || closedMethod == null
                )
                {
                    continue;
                }

                var parameters = closedMethod.GetParameters();

                if (parameters.Length == 0 || parameters.Length > 4)
                {
                    continue;
                }

                if (!parameters[0].ParameterType.IsAssignableFrom(typeof(IQueryable<TEntity>)))
                {
                    continue;
                }

                if (parameters.Length >= 2 && parameters[1].ParameterType != typeof(string))
                {
                    continue;
                }

                if (
                    parameters.Length >= 3
                    && !IsSupportedValuesParameter(parameters[2].ParameterType)
                )
                {
                    continue;
                }

                if (parameters.Length == 4 && parameters[3].ParameterType != typeof(object[]))
                {
                    continue;
                }

                if (!typeof(IQueryable<TEntity>).IsAssignableFrom(closedMethod.ReturnType))
                {
                    continue;
                }

                var args = new object?[parameters.Length];
                args[0] = query;

                if (parameters.Length >= 2)
                {
                    args[1] = filterTerm.Operator.Id;
                }

                if (parameters.Length >= 3)
                {
                    args[2] = CreateValuesArgument(parameters[2].ParameterType, filterTerm.Values);
                }

                if (parameters.Length == 4)
                {
                    args[3] = dataForCustomMethods;
                }

                var result = closedMethod.Invoke(customFilterMethods, args);

                if (result is IQueryable<TEntity> typedResult)
                {
                    filteredQuery = typedResult;
                    return true;
                }
            }

            return false;
        }

        private static bool TryCloseCustomMethod<TEntity>(
            MethodInfo method,
            out MethodInfo? closedMethod
        )
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
                closedMethod = method.MakeGenericMethod(typeof(TEntity));
                return true;
            }
            catch (ArgumentException)
            {
                closedMethod = null;
                return false;
            }
        }

        private static bool IsSupportedValuesParameter(Type valuesParameterType)
        {
            return valuesParameterType == typeof(string[])
                || valuesParameterType == typeof(IReadOnlyList<string>)
                || valuesParameterType == typeof(IEnumerable<string>);
        }

        private static object CreateValuesArgument(
            Type valuesParameterType,
            IReadOnlyList<string> values
        )
        {
            if (valuesParameterType == typeof(string[]))
            {
                return values.ToArray();
            }

            return values;
        }

        private Expression? BuildFilterExpression(PaginationFilterExpressionContext context)
        {
            foreach (var expressionProvider in _filterExpressionProviders)
            {
                if (
                    expressionProvider.TryBuildExpression(context, out var expression)
                    && expression != null
                )
                {
                    return expression;
                }
            }

            return null;
        }

        private IQueryable<TEntity> ApplySorts<TEntity>(
            IQueryable<TEntity> query,
            IReadOnlyList<PaginationSortTerm> sorts,
            object[]? dataForCustomMethods
        )
        {
            if (sorts.Count == 0)
            {
                return query;
            }

            IOrderedQueryable<TEntity>? orderedQuery = null;
            IQueryable<TEntity> currentQuery = query;

            foreach (var sort in sorts)
            {
                var sourceForSort = orderedQuery ?? currentQuery;
                var parameterExpression = Expression.Parameter(typeof(TEntity), "entity");

                if (
                    !TryBuildMemberExpression(
                        parameterExpression,
                        sort.Field,
                        out var memberExpression,
                        applyFilterRules: false
                    )
                    || memberExpression == null
                )
                {
                    if (
                        TryApplyCustomSortMethod(
                            sourceForSort,
                            sort.Field,
                            orderedQuery != null,
                            sort.Descending,
                            dataForCustomMethods,
                            out var customSortedQuery
                        )
                    )
                    {
                        currentQuery = customSortedQuery;
                        orderedQuery = customSortedQuery as IOrderedQueryable<TEntity>;
                    }

                    continue;
                }

                var keySelector = Expression.Lambda(memberExpression, parameterExpression);

                if (orderedQuery == null)
                {
                    orderedQuery =
                        (IOrderedQueryable<TEntity>)
                            InvokeOrderMethod(
                                sort.Descending ? OrderByDescendingMethod : OrderByMethod,
                                typeof(TEntity),
                                sourceForSort,
                                keySelector
                            );
                }
                else
                {
                    orderedQuery =
                        (IOrderedQueryable<TEntity>)
                            InvokeOrderMethod(
                                sort.Descending ? ThenByDescendingMethod : ThenByMethod,
                                typeof(TEntity),
                                sourceForSort,
                                keySelector
                            );
                }

                currentQuery = orderedQuery;
            }

            return orderedQuery ?? currentQuery;
        }

        private bool TryApplyCustomSortMethod<TEntity>(
            IQueryable<TEntity> query,
            string field,
            bool useThenBy,
            bool descending,
            object[]? dataForCustomMethods,
            out IQueryable<TEntity> sortedQuery
        )
        {
            sortedQuery = query;

            if (_customSortMethods.Count == 0)
            {
                return false;
            }

            foreach (var customSortMethods in _customSortMethods)
            {
                if (
                    TryInvokeCustomSortMethod(
                        query,
                        customSortMethods,
                        field,
                        useThenBy,
                        descending,
                        dataForCustomMethods,
                        out sortedQuery
                    )
                )
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryInvokeCustomSortMethod<TEntity>(
            IQueryable<TEntity> query,
            IPaginationCustomSortsMethods customSortMethods,
            string methodName,
            bool useThenBy,
            bool descending,
            object[]? dataForCustomMethods,
            out IQueryable<TEntity> sortedQuery
        )
        {
            sortedQuery = query;

            var methods = customSortMethods
                .GetType()
                .GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .Where(x => string.Equals(x.Name, methodName, StringComparison.OrdinalIgnoreCase));

            foreach (var method in methods)
            {
                if (
                    !TryCloseCustomMethod<TEntity>(method, out var closedMethod)
                    || closedMethod == null
                )
                {
                    continue;
                }

                var parameters = closedMethod.GetParameters();

                if (parameters.Length == 0 || parameters.Length > 4)
                {
                    continue;
                }

                if (!parameters[0].ParameterType.IsAssignableFrom(typeof(IQueryable<TEntity>)))
                {
                    continue;
                }

                if (parameters.Length >= 2 && parameters[1].ParameterType != typeof(bool))
                {
                    continue;
                }

                if (parameters.Length >= 3 && parameters[2].ParameterType != typeof(bool))
                {
                    continue;
                }

                if (parameters.Length == 4 && parameters[3].ParameterType != typeof(object[]))
                {
                    continue;
                }

                if (!typeof(IQueryable<TEntity>).IsAssignableFrom(closedMethod.ReturnType))
                {
                    continue;
                }

                var args = new object?[parameters.Length];
                args[0] = query;

                if (parameters.Length >= 2)
                {
                    args[1] = useThenBy;
                }

                if (parameters.Length >= 3)
                {
                    args[2] = descending;
                }

                if (parameters.Length == 4)
                {
                    args[3] = dataForCustomMethods;
                }

                var result = closedMethod.Invoke(customSortMethods, args);

                if (result is IQueryable<TEntity> typedResult)
                {
                    sortedQuery = typedResult;
                    return true;
                }
            }

            return false;
        }

        private static IQueryable<TEntity> ApplyPagination<TEntity>(
            IQueryable<TEntity> query,
            int page,
            int pageSize
        )
        {
            var skip = (page - 1) * pageSize;
            return query.Skip(skip).Take(pageSize);
        }

        private static IReadOnlyList<object?> ConvertValues(
            IReadOnlyList<string> rawValues,
            Type targetType
        )
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

        private static bool TryConvertValue(
            string rawValue,
            Type targetType,
            out object? convertedValue
        )
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
                var parsed = DateTimeOffset.TryParse(
                    rawValue,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out var dateTimeOffsetValue
                );
                convertedValue = parsed ? dateTimeOffsetValue : null;
                return parsed;
            }

            if (conversionType == typeof(DateTime))
            {
                var parsed = DateTime.TryParse(
                    rawValue,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out var dateTimeValue
                );
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
                convertedValue = Convert.ChangeType(
                    rawValue,
                    conversionType,
                    CultureInfo.InvariantCulture
                );
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
                    catch (FormatException) { }
                    catch (NotSupportedException) { }
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
            bool applyFilterRules = true
        )
        {
            memberExpression = source;

            var fieldSegments = fieldPath.Split(
                '.',
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
            );

            if (fieldSegments.Length == 0)
            {
                memberExpression = null;
                return false;
            }

            for (var i = 0; i < fieldSegments.Length; i++)
            {
                var segment = fieldSegments[i];
                var isLeafSegment = i == fieldSegments.Length - 1;
                var property = memberExpression
                    .Type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .FirstOrDefault(x =>
                        MatchesSegment(x, segment, applyFilterRules, isLeafSegment)
                    );

                if (property != null)
                {
                    memberExpression = Expression.Property(memberExpression, property);
                    continue;
                }

                var field = memberExpression
                    .Type.GetFields(BindingFlags.Public | BindingFlags.Instance)
                    .FirstOrDefault(x =>
                        MatchesSegment(x, segment, applyFilterRules, isLeafSegment)
                    );

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

        private static bool MatchesSegment(
            MemberInfo memberInfo,
            string segment,
            bool applyFilterRules,
            bool isLeafSegment
        )
        {
            var attribute = memberInfo.GetCustomAttribute<PaginationAttribute>();
            var memberNameMatches = string.Equals(
                memberInfo.Name,
                segment,
                StringComparison.OrdinalIgnoreCase
            );
            var attributeNameMatches =
                attribute != null
                && !string.IsNullOrWhiteSpace(attribute.Name)
                && string.Equals(attribute.Name, segment, StringComparison.OrdinalIgnoreCase);

            if (!memberNameMatches && !attributeNameMatches)
            {
                return false;
            }

            if (attribute == null)
            {
                return true;
            }

            if (isLeafSegment)
            {
                return applyFilterRules ? attribute.CanFilter : attribute.CanSort;
            }

            return applyFilterRules
                ? attribute.CanFilterSubProperties
                : attribute.CanSortSubProperties;
        }

        private static object InvokeOrderMethod(
            MethodInfo orderMethod,
            Type entityType,
            object source,
            LambdaExpression keySelector
        )
        {
            var closedMethod = orderMethod.MakeGenericMethod(entityType, keySelector.ReturnType);
            return closedMethod.Invoke(null, [source, keySelector])!;
        }

        private static MethodInfo GetOrderMethod(string methodName)
        {
            return typeof(Queryable)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Single(method =>
                    method.Name == methodName
                    && method.IsGenericMethodDefinition
                    && method.GetParameters().Length == 2
                );
        }
    }
}
