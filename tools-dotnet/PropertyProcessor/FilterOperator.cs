using Enum.Ext;
using System.Collections.Generic;
using System.Linq;
using tools_dotnet.PropertyProcessor.Filter;

namespace tools_dotnet.PropertyProcessor
{
    public sealed class FilterOperator : TypeSafeEnum<FilterOperator, int>
    {
        #region Mixed-Type

        public static readonly FilterOperator Equal = new FilterOperator(1, "==",
            new EqualFilter<bool>(),
            new EqualFilter<long>(),
            new EqualFilter<string>());

        public static readonly FilterOperator NotEqual = new FilterOperator(2, "!=",
            new NegateFilter<bool, bool>(new EqualFilter<bool>()),
            new NegateFilter<long, long>(new EqualFilter<long>()),
            new NegateFilter<string, string>(new EqualFilter<string>()));

        #endregion Mixed-Type

        #region Long

        public static readonly FilterOperator GreaterThan = new FilterOperator(3, ">", new LongFilter((lhs, rhs) => lhs > rhs));

        public static readonly FilterOperator LessThan = new FilterOperator(4, "<", new LongFilter((lhs, rhs) => lhs < rhs));

        public static readonly FilterOperator GreaterThanOrEqualTo = new FilterOperator(5, ">=", new LongFilter((lhs, rhs) => lhs >= rhs));

        public static readonly FilterOperator LessThanOrEqualTo = new FilterOperator(6, "<=", new LongFilter((lhs, rhs) => lhs <= rhs));

        #endregion Long

        #region String|Mixed-Array:Case-Sensitive

        public static readonly FilterOperator Contains = new FilterOperator(7, "@=",
                GenerateStringCombinationFilter(new StringContainsFilter()),
                new ArrayContainsFilter<bool>(),
                new ArrayContainsFilter<long>(),
                new ArrayContainsFilter<string>());

        public static readonly FilterOperator NotContains = new FilterOperator(8, "!@=",
            GenerateAndNegateStringCombinationFilter(new StringContainsFilter()),
            new NegateFilter<ICollection<bool>, bool>(new ArrayContainsFilter<bool>()),
            new NegateFilter<ICollection<long>, long>(new ArrayContainsFilter<long>()),
            new NegateFilter<ICollection<string>, string>(new ArrayContainsFilter<string>()));

        #endregion String|Mixed-Array:Case-Sensitive

        #region String:Case-Sensitive

        public static readonly FilterOperator StartsWith = new FilterOperator(9, "_=",
            GenerateStringCombinationFilter(new StringFilter((lhs, rhs) => lhs.StartsWith(rhs))));

        public static readonly FilterOperator NotStartsWith = new FilterOperator(10, "!_=",
            GenerateAndNegateStringCombinationFilter(new StringFilter((lhs, rhs) => lhs.StartsWith(rhs))));

        public static readonly FilterOperator EndsWith = new FilterOperator(11, "_-=",
            GenerateStringCombinationFilter(new StringFilter((lhs, rhs) => lhs.EndsWith(rhs))));

        public static readonly FilterOperator NotEndsWith = new FilterOperator(12, "!_-=",
            GenerateAndNegateStringCombinationFilter(new StringFilter((lhs, rhs) => lhs.StartsWith(rhs))));

        #endregion String:Case-Sensitive

        #region String:Case-Insensitive

        public static readonly FilterOperator CaseInsensitiveContains = new FilterOperator(13, "@=*",
            GenerateStringCombinationFilter(new StringFilter((lhs, rhs) => lhs.ToLowerInvariant().Contains(rhs.ToLowerInvariant()))));

        public static readonly FilterOperator CaseInsensitiveNotContains = new FilterOperator(14, "!@=*",
            GenerateAndNegateStringCombinationFilter(new StringFilter((lhs, rhs) => lhs.ToLowerInvariant().Contains(rhs.ToLowerInvariant()))));

        public static readonly FilterOperator CaseInsensitiveStartsWith = new FilterOperator(15, "_=*",
            GenerateStringCombinationFilter(new StringFilter((lhs, rhs) => lhs.ToLowerInvariant().StartsWith(rhs.ToLowerInvariant()))));

        public static readonly FilterOperator CaseInsensitiveNotStartsWith = new FilterOperator(16, "!_=*",
            GenerateAndNegateStringCombinationFilter(new StringFilter((lhs, rhs) => lhs.ToLowerInvariant().StartsWith(rhs.ToLowerInvariant()))));

        public static readonly FilterOperator CaseInsensitiveEndsWith = new FilterOperator(17, "_-=*",
            GenerateStringCombinationFilter(new StringFilter((lhs, rhs) => lhs.ToLowerInvariant().EndsWith(rhs.ToLowerInvariant()))));

        public static readonly FilterOperator CaseInsensitiveNotEndsWith = new FilterOperator(18, "!_-=*",
            GenerateAndNegateStringCombinationFilter(new StringFilter((lhs, rhs) => lhs.ToLowerInvariant().EndsWith(rhs.ToLowerInvariant()))));

        #endregion String:Case-Insensitive

        #region Helper

        public static IFilter[] GenerateStringCombinationFilter(IFilter<string, string> originalFilter)
        {
            return new IFilter[] {
                originalFilter,
                new StringArrayFilterLhs(originalFilter),
                new StringArrayFilterRhs(originalFilter),
                new StringArrayFilterBoth(originalFilter),
            };
        }

        public static IFilter[] GenerateAndNegateStringCombinationFilter(IFilter<string, string> originalFilter)
        {
            return new IFilter[] {
                new NegateFilter<string, string>(originalFilter),
                new NegateFilter<ICollection<string>, string>(new StringArrayFilterLhs(originalFilter)),
                new NegateFilter< string, ICollection<string>>(new StringArrayFilterRhs(originalFilter)),
                new NegateFilter<ICollection<string>, ICollection<string>>(new StringArrayFilterBoth(originalFilter)),
            };
        }

        #endregion Helper

        public string @Operator { get; }

        private IFilter[] _filters { get; }

        private FilterOperator(int id, string @operator, params IFilter[] filters) : base(id)
        {
            @Operator = @operator;
            _filters = filters;
        }

        private FilterOperator(int id, string @operator, IFilter[] filterArray, params IFilter[] filters) : base(id)
        {
            @Operator = @operator;
            _filters = filterArray.Concat(filters).ToArray();
        }

        public static IFilter<LHS, RHS>? GetFilter<LHS, RHS>(string @operator)
        {
            return List
                .Where(e => e.Operator.Equals(@operator))
                .SelectMany(x => x._filters)
                .Select(x => x as IFilter<LHS, RHS>)
                .SingleOrDefault(e => e != null);
        }

        public static bool? Apply<LHS, RHS>(string @operator, LHS lhs, RHS rhs)
        {
            var foundFilter = GetFilter<LHS, RHS>(@operator);

            if (foundFilter == null)
            {
                return null;
            }

            return foundFilter.Apply(lhs, rhs);
        }

        public IFilter<LHS, RHS>? GetFilter<LHS, RHS>()
        {
            return _filters
                .Select(x => x as IFilter<LHS, RHS>)
                .SingleOrDefault(e => e != null);
        }

        public bool? Apply<LHS, RHS>(LHS lhs, RHS rhs)
        {
            var foundFilter = GetFilter<LHS, RHS>();

            if (foundFilter == null)
            {
                return null;
            }

            return foundFilter.Apply(lhs, rhs);
        }
    }
}