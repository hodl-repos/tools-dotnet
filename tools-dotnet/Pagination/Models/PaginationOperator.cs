using System;
using System.Collections.Generic;
using System.Linq;
using Enum.Ext;

namespace tools_dotnet.Pagination.Models
{
    /// <summary>
    /// Type-safe operator list used in filter parsing and expression generation.
    /// </summary>
    public sealed class PaginationOperator : TypeSafeNameEnum<PaginationOperator, string>
    {
        /// <summary>
        /// Equality operator.
        /// </summary>
        public static readonly PaginationOperator Equal = new("==", nameof(Equal));

        /// <summary>
        /// Case-insensitive equality operator.
        /// </summary>
        public static readonly PaginationOperator EqualCaseInsensitive = new(
            "==*",
            nameof(EqualCaseInsensitive)
        );

        /// <summary>
        /// Not-equal operator.
        /// </summary>
        public static readonly PaginationOperator NotEquals = new("!=", nameof(NotEquals));

        /// <summary>
        /// Case-insensitive not-equal operator.
        /// </summary>
        public static readonly PaginationOperator NotEqualsCaseInsensitive = new(
            "!=*",
            nameof(NotEqualsCaseInsensitive)
        );

        /// <summary>
        /// Greater-than operator.
        /// </summary>
        public static readonly PaginationOperator GreaterThan = new(">", nameof(GreaterThan));

        /// <summary>
        /// Greater-than-or-equal operator.
        /// </summary>
        public static readonly PaginationOperator GreaterThanOrEqual = new(
            ">=",
            nameof(GreaterThanOrEqual)
        );

        /// <summary>
        /// Less-than operator.
        /// </summary>
        public static readonly PaginationOperator LessThan = new("<", nameof(LessThan));

        /// <summary>
        /// Less-than-or-equal operator.
        /// </summary>
        public static readonly PaginationOperator LessThanOrEqual = new(
            "<=",
            nameof(LessThanOrEqual)
        );

        /// <summary>
        /// Contains operator.
        /// </summary>
        public static readonly PaginationOperator Contains = new("@=", nameof(Contains));

        /// <summary>
        /// Case-insensitive contains operator.
        /// </summary>
        public static readonly PaginationOperator ContainsCaseInsensitive = new(
            "@=*",
            nameof(ContainsCaseInsensitive)
        );

        /// <summary>
        /// Not-contains operator.
        /// </summary>
        public static readonly PaginationOperator NotContains = new("!@=", nameof(NotContains));

        /// <summary>
        /// Case-insensitive not-contains operator.
        /// </summary>
        public static readonly PaginationOperator NotContainsCaseInsensitive = new(
            "!@=*",
            nameof(NotContainsCaseInsensitive)
        );

        /// <summary>
        /// Starts-with operator.
        /// </summary>
        public static readonly PaginationOperator StartsWith = new("_=", nameof(StartsWith));

        /// <summary>
        /// Case-insensitive starts-with operator.
        /// </summary>
        public static readonly PaginationOperator StartsWithCaseInsensitive = new(
            "_=*",
            nameof(StartsWithCaseInsensitive)
        );

        /// <summary>
        /// Not-starts-with operator.
        /// </summary>
        public static readonly PaginationOperator NotStartsWith = new("!_=", nameof(NotStartsWith));

        /// <summary>
        /// Case-insensitive not-starts-with operator.
        /// </summary>
        public static readonly PaginationOperator NotStartsWithCaseInsensitive = new(
            "!_=*",
            nameof(NotStartsWithCaseInsensitive)
        );

        /// <summary>
        /// Ends-with operator.
        /// </summary>
        public static readonly PaginationOperator EndsWith = new("_-=", nameof(EndsWith));

        /// <summary>
        /// Case-insensitive ends-with operator.
        /// </summary>
        public static readonly PaginationOperator EndsWithCaseInsensitive = new(
            "_-=*",
            nameof(EndsWithCaseInsensitive)
        );

        /// <summary>
        /// Not-ends-with operator.
        /// </summary>
        public static readonly PaginationOperator NotEndsWith = new("!_-=", nameof(NotEndsWith));

        /// <summary>
        /// Case-insensitive not-ends-with operator.
        /// </summary>
        public static readonly PaginationOperator NotEndsWithCaseInsensitive = new(
            "!_-=*",
            nameof(NotEndsWithCaseInsensitive)
        );

        private static readonly Lazy<
            IReadOnlyDictionary<string, PaginationOperator>
        > _operatorByToken = new(() =>
            List.ToDictionary(x => x.Id, x => x, StringComparer.Ordinal)
        );

        private static readonly Lazy<IReadOnlyList<string>> _tokensByLength = new(() =>
            List.Select(x => x.Id).OrderByDescending(x => x.Length).ToArray()
        );

        private PaginationOperator(string id, string name)
            : base(id, name) { }

        /// <summary>
        /// Gets all known operators.
        /// </summary>
        public static IReadOnlyCollection<PaginationOperator> Values => List;

        /// <summary>
        /// Gets operator tokens sorted by length, longest first.
        /// </summary>
        public static IReadOnlyList<string> TokensByLength => _tokensByLength.Value;

        /// <summary>
        /// Gets a value indicating whether this operator is negated.
        /// </summary>
        public bool IsNegated =>
            this == NotEquals
            || this == NotEqualsCaseInsensitive
            || this == NotContains
            || this == NotContainsCaseInsensitive
            || this == NotStartsWith
            || this == NotStartsWithCaseInsensitive
            || this == NotEndsWith
            || this == NotEndsWithCaseInsensitive;

        /// <summary>
        /// Gets a value indicating whether this operator is string-specific.
        /// </summary>
        public bool IsStringOperator =>
            this == EqualCaseInsensitive
            || this == NotEqualsCaseInsensitive
            || this == Contains
            || this == ContainsCaseInsensitive
            || this == NotContains
            || this == NotContainsCaseInsensitive
            || this == StartsWith
            || this == StartsWithCaseInsensitive
            || this == NotStartsWith
            || this == NotStartsWithCaseInsensitive
            || this == EndsWith
            || this == EndsWithCaseInsensitive
            || this == NotEndsWith
            || this == NotEndsWithCaseInsensitive;

        /// <summary>
        /// Gets a value indicating whether this operator is case-insensitive.
        /// </summary>
        public bool IsCaseInsensitive =>
            this == EqualCaseInsensitive
            || this == NotEqualsCaseInsensitive
            || this == ContainsCaseInsensitive
            || this == NotContainsCaseInsensitive
            || this == StartsWithCaseInsensitive
            || this == NotStartsWithCaseInsensitive
            || this == EndsWithCaseInsensitive
            || this == NotEndsWithCaseInsensitive;

        /// <summary>
        /// Tries to resolve an operator from a raw token.
        /// </summary>
        /// <param name="token">Operator token from the request.</param>
        /// <param name="op">Resolved operator, if found.</param>
        /// <returns><see langword="true"/> when the token is known; otherwise <see langword="false"/>.</returns>
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
