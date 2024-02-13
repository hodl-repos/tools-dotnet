using Enum.Ext;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using tools_dotnet.PropertyProcessor.Filter;

namespace tools_dotnet.PropertyProcessor
{
    public sealed class FilterOperatorEnum : TypeSafeEnum<FilterOperatorEnum, int>
    {
        #region Mixed-Type

        public static readonly FilterOperatorEnum Equal = new FilterOperatorEnum(1, "==",
            new EqualFilter<bool>(),
            new EqualFilter<long>(),
            new EqualFilter<string>(),
            new RegexFilter());

        public static readonly FilterOperatorEnum NotEqual = new FilterOperatorEnum(2, "!=",
            new NegateFilter<bool, bool>(new EqualFilter<bool>()),
            new NegateFilter<long, long>(new EqualFilter<long>()),
            new NegateFilter<string, string>(new EqualFilter<string>()),
            new NegateFilter<string, Regex>(new RegexFilter()));

        #endregion Mixed-Type

        #region Long

        public static readonly FilterOperatorEnum GreaterThan = new FilterOperatorEnum(3, ">", new LongFilter((lhs, rhs) => lhs > rhs));

        public static readonly FilterOperatorEnum LessThan = new FilterOperatorEnum(4, "<", new LongFilter((lhs, rhs) => lhs < rhs));

        public static readonly FilterOperatorEnum GreaterThanOrEqualTo = new FilterOperatorEnum(5, ">=", new LongFilter((lhs, rhs) => lhs >= rhs));

        public static readonly FilterOperatorEnum LessThanOrEqualTo = new FilterOperatorEnum(6, "<=", new LongFilter((lhs, rhs) => lhs <= rhs));

        #endregion Long

        #region String|Mixed-Array:Case-Sensitive

        public static readonly FilterOperatorEnum Contains = new FilterOperatorEnum(7, "@=",
                GenerateStringCombinationFilter(new StringContainsFilter()),
                new RegexFilter(),
                new RegexArrayFilter(),
                new ArrayContainsFilter<bool>(),
                new ArrayContainsFilter<long>(),
                new ArrayContainsFilter<string>());

        public static readonly FilterOperatorEnum NotContains = new FilterOperatorEnum(8, "!@=",
            GenerateAndNegateStringCombinationFilter(new StringContainsFilter()),
            new NegateFilter<string, Regex>(new RegexFilter()),
            new NegateFilter<ICollection<string>, Regex>(new RegexArrayFilter()),
            new NegateFilter<ICollection<bool>, bool>(new ArrayContainsFilter<bool>()),
            new NegateFilter<ICollection<long>, long>(new ArrayContainsFilter<long>()),
            new NegateFilter<ICollection<string>, string>(new ArrayContainsFilter<string>()));

        #endregion String|Mixed-Array:Case-Sensitive

        #region String:Case-Sensitive

        public static readonly FilterOperatorEnum StartsWith = new FilterOperatorEnum(9, "_=",
            GenerateStringCombinationFilter(new StringFilter((lhs, rhs) => lhs.StartsWith(rhs))));

        public static readonly FilterOperatorEnum NotStartsWith = new FilterOperatorEnum(10, "!_=",
            GenerateAndNegateStringCombinationFilter(new StringFilter((lhs, rhs) => lhs.StartsWith(rhs))));

        public static readonly FilterOperatorEnum EndsWith = new FilterOperatorEnum(11, "_-=",
            GenerateStringCombinationFilter(new StringFilter((lhs, rhs) => lhs.EndsWith(rhs))));

        public static readonly FilterOperatorEnum NotEndsWith = new FilterOperatorEnum(12, "!_-=",
            GenerateAndNegateStringCombinationFilter(new StringFilter((lhs, rhs) => lhs.StartsWith(rhs))));

        #endregion String:Case-Sensitive

        #region String:Case-Insensitive

        public static readonly FilterOperatorEnum CaseInsensitiveContains = new FilterOperatorEnum(13, "@=*",
            GenerateStringCombinationFilter(new StringFilter((lhs, rhs) => lhs.ToLowerInvariant().Contains(rhs.ToLowerInvariant()))));

        public static readonly FilterOperatorEnum CaseInsensitiveNotContains = new FilterOperatorEnum(14, "!@=*",
            GenerateAndNegateStringCombinationFilter(new StringFilter((lhs, rhs) => lhs.ToLowerInvariant().Contains(rhs.ToLowerInvariant()))));

        public static readonly FilterOperatorEnum CaseInsensitiveStartsWith = new FilterOperatorEnum(15, "_=*",
            GenerateStringCombinationFilter(new StringFilter((lhs, rhs) => lhs.ToLowerInvariant().StartsWith(rhs.ToLowerInvariant()))));

        public static readonly FilterOperatorEnum CaseInsensitiveNotStartsWith = new FilterOperatorEnum(16, "!_=*",
            GenerateAndNegateStringCombinationFilter(new StringFilter((lhs, rhs) => lhs.ToLowerInvariant().StartsWith(rhs.ToLowerInvariant()))));

        public static readonly FilterOperatorEnum CaseInsensitiveEndsWith = new FilterOperatorEnum(17, "_-=*",
            GenerateStringCombinationFilter(new StringFilter((lhs, rhs) => lhs.ToLowerInvariant().EndsWith(rhs.ToLowerInvariant()))));

        public static readonly FilterOperatorEnum CaseInsensitiveNotEndsWith = new FilterOperatorEnum(18, "!_-=*",
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

        private FilterOperatorEnum(int id, string @operator, params IFilter[] filters) : base(id)
        {
            @Operator = @operator;
            _filters = filters;
        }

        private FilterOperatorEnum(int id, string @operator, IFilter[] filterArray, params IFilter[] filters) : base(id)
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