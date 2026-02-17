using Enum.Ext;
using System;
using System.Collections.Generic;
using System.Linq;

namespace tools_dotnet.Pagination.Models
{
    public sealed class PaginationOperator : TypeSafeNameEnum<PaginationOperator, string>
    {
        public static readonly PaginationOperator Equal = new("==", nameof(Equal));
        public static readonly PaginationOperator EqualCaseInsensitive = new("==*", nameof(EqualCaseInsensitive));
        public static readonly PaginationOperator NotEquals = new("!=", nameof(NotEquals));
        public static readonly PaginationOperator NotEqualsCaseInsensitive = new("!=*", nameof(NotEqualsCaseInsensitive));
        public static readonly PaginationOperator GreaterThan = new(">", nameof(GreaterThan));
        public static readonly PaginationOperator GreaterThanOrEqual = new(">=", nameof(GreaterThanOrEqual));
        public static readonly PaginationOperator LessThan = new("<", nameof(LessThan));
        public static readonly PaginationOperator LessThanOrEqual = new("<=", nameof(LessThanOrEqual));
        public static readonly PaginationOperator Contains = new("@=", nameof(Contains));
        public static readonly PaginationOperator ContainsCaseInsensitive = new("@=*", nameof(ContainsCaseInsensitive));
        public static readonly PaginationOperator NotContains = new("!@=", nameof(NotContains));
        public static readonly PaginationOperator NotContainsCaseInsensitive = new("!@=*", nameof(NotContainsCaseInsensitive));
        public static readonly PaginationOperator StartsWith = new("_=", nameof(StartsWith));
        public static readonly PaginationOperator StartsWithCaseInsensitive = new("_=*", nameof(StartsWithCaseInsensitive));
        public static readonly PaginationOperator NotStartsWith = new("!_=", nameof(NotStartsWith));
        public static readonly PaginationOperator NotStartsWithCaseInsensitive = new("!_=*", nameof(NotStartsWithCaseInsensitive));
        public static readonly PaginationOperator EndsWith = new("_-=", nameof(EndsWith));
        public static readonly PaginationOperator EndsWithCaseInsensitive = new("_-=*", nameof(EndsWithCaseInsensitive));
        public static readonly PaginationOperator NotEndsWith = new("!_-=", nameof(NotEndsWith));
        public static readonly PaginationOperator NotEndsWithCaseInsensitive = new("!_-=*", nameof(NotEndsWithCaseInsensitive));

        private static readonly Lazy<IReadOnlyDictionary<string, PaginationOperator>> _operatorByToken =
            new(() => List.ToDictionary(x => x.Id, x => x, StringComparer.Ordinal));

        private static readonly Lazy<IReadOnlyList<string>> _tokensByLength =
            new(() => List
                .Select(x => x.Id)
                .OrderByDescending(x => x.Length)
                .ToArray());

        private PaginationOperator(string id, string name)
            : base(id, name)
        {
        }

        public static IReadOnlyCollection<PaginationOperator> Values => List;

        public static IReadOnlyList<string> TokensByLength => _tokensByLength.Value;

        public bool IsNegated =>
            this == NotEquals ||
            this == NotEqualsCaseInsensitive ||
            this == NotContains ||
            this == NotContainsCaseInsensitive ||
            this == NotStartsWith ||
            this == NotStartsWithCaseInsensitive ||
            this == NotEndsWith ||
            this == NotEndsWithCaseInsensitive;

        public bool IsStringOperator =>
            this == EqualCaseInsensitive ||
            this == NotEqualsCaseInsensitive ||
            this == Contains ||
            this == ContainsCaseInsensitive ||
            this == NotContains ||
            this == NotContainsCaseInsensitive ||
            this == StartsWith ||
            this == StartsWithCaseInsensitive ||
            this == NotStartsWith ||
            this == NotStartsWithCaseInsensitive ||
            this == EndsWith ||
            this == EndsWithCaseInsensitive ||
            this == NotEndsWith ||
            this == NotEndsWithCaseInsensitive;

        public bool IsCaseInsensitive =>
            this == EqualCaseInsensitive ||
            this == NotEqualsCaseInsensitive ||
            this == ContainsCaseInsensitive ||
            this == NotContainsCaseInsensitive ||
            this == StartsWithCaseInsensitive ||
            this == NotStartsWithCaseInsensitive ||
            this == EndsWithCaseInsensitive ||
            this == NotEndsWithCaseInsensitive;

        public static bool TryFromToken(string token, out PaginationOperator? op)
        {
            if (_operatorByToken.Value.TryGetValue(token, out var found))
            {
                op = found;
                return true;
            }

            op = null;
            return false;
        }
    }
}
