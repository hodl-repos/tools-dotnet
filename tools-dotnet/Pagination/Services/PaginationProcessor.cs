using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using tools_dotnet.Exceptions;
using tools_dotnet.Pagination.Attributes;
using tools_dotnet.Pagination.Models;

namespace tools_dotnet.Pagination.Services
{
    /// <summary>
    /// Applies filtering, sorting, and pagination to an <see cref="IQueryable{T}"/>.
    /// </summary>
    public class PaginationProcessor : IPaginationProcessor, IDeserializedPaginationProcessor
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
        private readonly ConcurrentDictionary<MemberPathCacheKey, MemberPathCacheEntry> _memberPathCache =
            new();
        private readonly ConcurrentDictionary<CustomMethodCacheKey, FilterMethodCacheEntry> _customFilterMethodCache =
            new();
        private readonly ConcurrentDictionary<CustomMethodCacheKey, SortMethodCacheEntry> _customSortMethodCache =
            new();
        private readonly ConcurrentDictionary<Type, IReadOnlyList<PaginationSortTerm>> _defaultSortCache =
            new();

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

            return ApplyInternal(
                Deserialize(model),
                source,
                dataForCustomMethods,
                applyFiltering,
                applySorting,
                applyPagination
            );
        }

        DeserializedPaginationModel IDeserializedPaginationProcessor.Deserialize(PaginationModel model)
        {
            return Deserialize(model);
        }

        IQueryable<TEntity> IDeserializedPaginationProcessor.Apply<TEntity>(
            DeserializedPaginationModel model,
            IQueryable<TEntity> source,
            object[]? dataForCustomMethods,
            bool applyFiltering,
            bool applySorting,
            bool applyPagination
        )
        {
            return ApplyInternal(
                model,
                source,
                dataForCustomMethods,
                applyFiltering,
                applySorting,
                applyPagination
            );
        }

        private DeserializedPaginationModel Deserialize(PaginationModel model)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            return _deserializer.Deserialize(model);
        }

        private IQueryable<TEntity> ApplyInternal<TEntity>(
            DeserializedPaginationModel model,
            IQueryable<TEntity> source,
            object[]? dataForCustomMethods,
            bool applyFiltering,
            bool applySorting,
            bool applyPagination
        )
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var query = source;

            if (applyFiltering)
            {
                query = ApplyFilters(query, model.Filters, dataForCustomMethods);
            }

            if (applySorting)
            {
                var sorts = model.Sorts.Count == 0
                    ? GetDefaultSorts(typeof(TEntity))
                    : model.Sorts;
                query = ApplySorts(query, sorts, dataForCustomMethods);
            }

            if (applyPagination)
            {
                query = ApplyPagination(query, model.Page, model.PageSize);
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
                    var memberResolution = TryBuildMemberExpression(
                        parameterExpression,
                        field,
                        out var memberExpression
                    );

                    if (memberResolution != MemberPathResolution.Found)
                    {
                        if (
                            memberResolution == MemberPathResolution.Unknown
                            &&
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
                            continue;
                        }

                        if (memberResolution == MemberPathResolution.Forbidden)
                        {
                            throw InvalidPaginationFilterException.FieldNotFilterable(field);
                        }

                        throw InvalidPaginationFilterException.UnknownField(field);
                    }

                    var resolvedMemberExpression = memberExpression!;
                    var typedValues = ConvertValues(
                        filter.Values,
                        resolvedMemberExpression.Type,
                        field
                    );

                    var context = new PaginationFilterExpressionContext(
                        parameterExpression,
                        resolvedMemberExpression,
                        filter,
                        typedValues,
                        dataForCustomMethods
                    );

                    var memberFilterExpression = BuildFilterExpression(context);

                    if (memberFilterExpression == null)
                    {
                        throw InvalidPaginationFilterException.UnsupportedOperator(
                            field,
                            filter.Operator,
                            resolvedMemberExpression.Type
                        );
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

        private bool TryInvokeCustomFilterMethod<TEntity>(
            IQueryable<TEntity> query,
            IPaginationCustomFilterMethods customFilterMethods,
            string methodName,
            PaginationFilterTerm filterTerm,
            object[]? dataForCustomMethods,
            out IQueryable<TEntity> filteredQuery
        )
        {
            filteredQuery = query;

            var methodEntry = GetCustomFilterMethodEntry<TEntity>(customFilterMethods, methodName);

            if (!methodEntry.Found || methodEntry.Method == null)
            {
                return false;
            }

            var args = new object?[methodEntry.ParameterCount];
            args[0] = query;

            if (methodEntry.ParameterCount >= 2)
            {
                args[1] = filterTerm.Operator.Id;
            }

            if (methodEntry.ParameterCount >= 3 && methodEntry.ValuesParameterType != null)
            {
                args[2] = CreateValuesArgument(methodEntry.ValuesParameterType, filterTerm.Values);
            }

            if (methodEntry.ParameterCount == 4)
            {
                args[3] = dataForCustomMethods;
            }

            var result = methodEntry.Method.Invoke(customFilterMethods, args);

            if (result is IQueryable<TEntity> typedResult)
            {
                filteredQuery = typedResult;
                return true;
            }

            return false;
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

                var memberResolution = TryBuildMemberExpression(
                    parameterExpression,
                    sort.Field,
                    out var memberExpression,
                        applyFilterRules: false
                );

                if (memberResolution != MemberPathResolution.Found)
                {
                    if (
                        memberResolution == MemberPathResolution.Unknown
                        &&
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
                        continue;
                    }

                    if (memberResolution == MemberPathResolution.Forbidden)
                    {
                        throw InvalidPaginationSortException.FieldNotSortable(sort.Field);
                    }

                    throw InvalidPaginationSortException.UnknownField(sort.Field);
                }

                var keySelector = Expression.Lambda(memberExpression!, parameterExpression);

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

        private bool TryInvokeCustomSortMethod<TEntity>(
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

            var methodEntry = GetCustomSortMethodEntry<TEntity>(customSortMethods, methodName);

            if (!methodEntry.Found || methodEntry.Method == null)
            {
                return false;
            }

            var args = new object?[methodEntry.ParameterCount];
            args[0] = query;

            if (methodEntry.ParameterCount >= 2)
            {
                args[1] = useThenBy;
            }

            if (methodEntry.ParameterCount >= 3)
            {
                args[2] = descending;
            }

            if (methodEntry.ParameterCount == 4)
            {
                args[3] = dataForCustomMethods;
            }

            var result = methodEntry.Method.Invoke(customSortMethods, args);

            if (result is IQueryable<TEntity> typedResult)
            {
                sortedQuery = typedResult;
                return true;
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

        private IReadOnlyList<PaginationSortTerm> GetDefaultSorts(Type entityType)
        {
            return _defaultSortCache.GetOrAdd(entityType, static type =>
            {
                var result = new List<PaginationSortTerm>();
                CollectDefaultSorts(
                    type,
                    prefix: null,
                    result,
                    activePathTypes: new HashSet<Type>()
                );
                return result;
            });
        }

        private static void CollectDefaultSorts(
            Type modelType,
            string? prefix,
            ICollection<PaginationSortTerm> result,
            ISet<Type> activePathTypes
        )
        {
            if (!activePathTypes.Add(modelType))
            {
                return;
            }

            try
            {
                var members = modelType
                    .GetMembers(BindingFlags.Public | BindingFlags.Instance)
                    .Where(x =>
                        x.MemberType == MemberTypes.Property || x.MemberType == MemberTypes.Field
                    );

                foreach (var member in members)
                {
                    var attribute = member.GetCustomAttribute<PaginationAttribute>();

                    if (attribute == null)
                    {
                        continue;
                    }

                    var memberName = string.IsNullOrWhiteSpace(attribute.Name)
                        ? member.Name
                        : attribute.Name!;
                    var fieldPath = string.IsNullOrWhiteSpace(prefix)
                        ? memberName
                        : $"{prefix}.{memberName}";

                    if (attribute.IsDefaultSorted && attribute.CanSort)
                    {
                        result.Add(
                            new PaginationSortTerm(
                                fieldPath,
                                attribute.DefaultSortDescending
                            )
                        );
                    }

                    if (!attribute.CanSortSubProperties)
                    {
                        continue;
                    }

                    var memberType = GetMemberType(member);

                    if (!CanTraverseDefaultSortMembers(memberType))
                    {
                        continue;
                    }

                    var nestedType = Nullable.GetUnderlyingType(memberType) ?? memberType;
                    CollectDefaultSorts(nestedType, fieldPath, result, activePathTypes);
                }
            }
            finally
            {
                activePathTypes.Remove(modelType);
            }
        }

        private static bool CanTraverseDefaultSortMembers(Type memberType)
        {
            var type = Nullable.GetUnderlyingType(memberType) ?? memberType;

            return type != typeof(string)
                && !type.IsPrimitive
                && !type.IsEnum
                && type != typeof(decimal)
                && type != typeof(Guid)
                && type != typeof(DateTime)
                && type != typeof(DateTimeOffset)
                && type != typeof(DateOnly)
                && type != typeof(TimeOnly)
                && type != typeof(TimeSpan);
        }

        private static IReadOnlyList<object?> ConvertValues(
            IReadOnlyList<string> rawValues,
            Type targetType,
            string field
        )
        {
            var values = new List<object?>();

            foreach (var rawValue in rawValues)
            {
                if (TryConvertValue(rawValue, targetType, out var converted))
                {
                    values.Add(converted);
                    continue;
                }

                throw InvalidPaginationFilterException.FailedToParseValue(
                    field,
                    rawValue,
                    targetType
                );
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

        private MemberPathResolution TryBuildMemberExpression(
            Expression source,
            string fieldPath,
            out Expression? memberExpression,
            bool applyFilterRules = true
        )
        {
            var memberPath = _memberPathCache.GetOrAdd(
                new MemberPathCacheKey(
                    source.Type,
                    NormalizeCacheLookupKey(fieldPath),
                    applyFilterRules
                ),
                static cacheKey => BuildMemberPath(cacheKey)
            );

            if (memberPath.Resolution != MemberPathResolution.Found)
            {
                memberExpression = null;
                return memberPath.Resolution;
            }

            memberExpression = source;

            foreach (var member in memberPath.Members)
            {
                memberExpression = CreateMemberAccessExpression(memberExpression, member);
            }

            return MemberPathResolution.Found;
        }

        private FilterMethodCacheEntry GetCustomFilterMethodEntry<TEntity>(
            IPaginationCustomFilterMethods customFilterMethods,
            string methodName
        )
        {
            return _customFilterMethodCache.GetOrAdd(
                new CustomMethodCacheKey(
                    customFilterMethods.GetType(),
                    typeof(TEntity),
                    NormalizeCacheLookupKey(methodName)
                ),
                static cacheKey => ResolveCustomFilterMethod(cacheKey)
            );
        }

        private SortMethodCacheEntry GetCustomSortMethodEntry<TEntity>(
            IPaginationCustomSortsMethods customSortMethods,
            string methodName
        )
        {
            return _customSortMethodCache.GetOrAdd(
                new CustomMethodCacheKey(
                    customSortMethods.GetType(),
                    typeof(TEntity),
                    NormalizeCacheLookupKey(methodName)
                ),
                static cacheKey => ResolveCustomSortMethod(cacheKey)
            );
        }

        private static MemberPathCacheEntry BuildMemberPath(MemberPathCacheKey cacheKey)
        {
            var fieldSegments = cacheKey.FieldPath.Split(
                '.',
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
            );

            if (fieldSegments.Length == 0)
            {
                return MemberPathCacheEntry.Unknown;
            }

            var members = new MemberInfo[fieldSegments.Length];
            var currentType = cacheKey.SourceType;

            for (var i = 0; i < fieldSegments.Length; i++)
            {
                var segment = fieldSegments[i];
                var isLeafSegment = i == fieldSegments.Length - 1;
                var candidates = FindMatchingMembers(currentType, segment);

                if (candidates.Count == 0)
                {
                    return MemberPathCacheEntry.Unknown;
                }

                var member = candidates.FirstOrDefault(candidate =>
                    IsMemberAllowed(candidate, cacheKey.ApplyFilterRules, isLeafSegment)
                );

                if (member == null)
                {
                    return MemberPathCacheEntry.Forbidden;
                }

                members[i] = member;
                currentType = GetMemberType(member);
            }

            return new MemberPathCacheEntry(members);
        }

        private static IReadOnlyList<MemberInfo> FindMatchingMembers(
            Type sourceType,
            string segment
        )
        {
            return sourceType
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Cast<MemberInfo>()
                .Concat(
                    sourceType
                .GetFields(BindingFlags.Public | BindingFlags.Instance)
                        .Cast<MemberInfo>()
                )
                .Where(x => MatchesSegmentName(x, segment))
                .ToArray();
        }

        private static FilterMethodCacheEntry ResolveCustomFilterMethod(
            CustomMethodCacheKey cacheKey
        )
        {
            var queryableType = typeof(IQueryable<>).MakeGenericType(cacheKey.EntityType);

            foreach (
                var method in cacheKey
                    .ContainerType.GetMethods(BindingFlags.Instance | BindingFlags.Public)
                    .Where(x =>
                        string.Equals(x.Name, cacheKey.MethodName, StringComparison.OrdinalIgnoreCase)
                    )
            )
            {
                if (
                    !TryCloseCustomMethod(method, cacheKey.EntityType, out var closedMethod)
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

                if (!parameters[0].ParameterType.IsAssignableFrom(queryableType))
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

                if (!closedMethod.ReturnType.IsAssignableTo(queryableType))
                {
                    continue;
                }

                return new FilterMethodCacheEntry(
                    closedMethod,
                    parameters.Length,
                    parameters.Length >= 3 ? parameters[2].ParameterType : null
                );
            }

            return FilterMethodCacheEntry.Missing;
        }

        private static SortMethodCacheEntry ResolveCustomSortMethod(CustomMethodCacheKey cacheKey)
        {
            var queryableType = typeof(IQueryable<>).MakeGenericType(cacheKey.EntityType);

            foreach (
                var method in cacheKey
                    .ContainerType.GetMethods(BindingFlags.Instance | BindingFlags.Public)
                    .Where(x =>
                        string.Equals(x.Name, cacheKey.MethodName, StringComparison.OrdinalIgnoreCase)
                    )
            )
            {
                if (
                    !TryCloseCustomMethod(method, cacheKey.EntityType, out var closedMethod)
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

                if (!parameters[0].ParameterType.IsAssignableFrom(queryableType))
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

                if (!closedMethod.ReturnType.IsAssignableTo(queryableType))
                {
                    continue;
                }

                return new SortMethodCacheEntry(closedMethod, parameters.Length);
            }

            return SortMethodCacheEntry.Missing;
        }

        private static bool TryCloseCustomMethod(
            MethodInfo method,
            Type entityType,
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
                closedMethod = method.MakeGenericMethod(entityType);
                return true;
            }
            catch (ArgumentException)
            {
                closedMethod = null;
                return false;
            }
        }

        private static bool MatchesSegmentName(
            MemberInfo memberInfo,
            string segment
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

            return memberNameMatches || attributeNameMatches;
        }

        private static bool IsMemberAllowed(
            MemberInfo memberInfo,
            bool applyFilterRules,
            bool isLeafSegment
        )
        {
            var attribute = memberInfo.GetCustomAttribute<PaginationAttribute>();

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

        private static Expression CreateMemberAccessExpression(
            Expression source,
            MemberInfo memberInfo
        )
        {
            return memberInfo switch
            {
                PropertyInfo propertyInfo => Expression.Property(source, propertyInfo),
                FieldInfo fieldInfo => Expression.Field(source, fieldInfo),
                _ => throw new InvalidOperationException(
                    $"Unsupported member type '{memberInfo.MemberType}'."
                ),
            };
        }

        private static Type GetMemberType(MemberInfo memberInfo)
        {
            return memberInfo switch
            {
                PropertyInfo propertyInfo => propertyInfo.PropertyType,
                FieldInfo fieldInfo => fieldInfo.FieldType,
                _ => throw new InvalidOperationException(
                    $"Unsupported member type '{memberInfo.MemberType}'."
                ),
            };
        }

        private static string NormalizeCacheLookupKey(string value)
        {
            return value.Trim().ToUpperInvariant();
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

        private readonly record struct MemberPathCacheKey(
            Type SourceType,
            string FieldPath,
            bool ApplyFilterRules
        );

        private readonly record struct CustomMethodCacheKey(
            Type ContainerType,
            Type EntityType,
            string MethodName
        );

        private sealed class MemberPathCacheEntry
        {
            public static readonly MemberPathCacheEntry Unknown = new(
                MemberPathResolution.Unknown,
                Array.Empty<MemberInfo>()
            );
            public static readonly MemberPathCacheEntry Forbidden = new(
                MemberPathResolution.Forbidden,
                Array.Empty<MemberInfo>()
            );

            public MemberPathCacheEntry(IReadOnlyList<MemberInfo> members)
                : this(MemberPathResolution.Found, members) { }

            private MemberPathCacheEntry(
                MemberPathResolution resolution,
                IReadOnlyList<MemberInfo> members
            )
            {
                Resolution = resolution;
                Members = members;
            }

            public MemberPathResolution Resolution { get; }

            public IReadOnlyList<MemberInfo> Members { get; }
        }

        private enum MemberPathResolution
        {
            Found,
            Unknown,
            Forbidden,
        }

        private sealed class FilterMethodCacheEntry
        {
            public static readonly FilterMethodCacheEntry Missing = new(null, 0, null);

            public FilterMethodCacheEntry(
                MethodInfo? method,
                int parameterCount,
                Type? valuesParameterType
            )
            {
                Method = method;
                ParameterCount = parameterCount;
                ValuesParameterType = valuesParameterType;
            }

            public MethodInfo? Method { get; }

            public int ParameterCount { get; }

            public Type? ValuesParameterType { get; }

            public bool Found => Method != null;
        }

        private sealed class SortMethodCacheEntry
        {
            public static readonly SortMethodCacheEntry Missing = new(null, 0);

            public SortMethodCacheEntry(MethodInfo? method, int parameterCount)
            {
                Method = method;
                ParameterCount = parameterCount;
            }

            public MethodInfo? Method { get; }

            public int ParameterCount { get; }

            public bool Found => Method != null;
        }
    }
}
