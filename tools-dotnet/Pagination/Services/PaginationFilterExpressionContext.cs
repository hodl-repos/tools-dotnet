using tools_dotnet.Pagination.Models;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace tools_dotnet.Pagination.Services
{
    /// <summary>
    /// Context passed to filter expression providers.
    /// </summary>
    public sealed class PaginationFilterExpressionContext
    {
        /// <summary>
        /// Creates a filter expression context.
        /// </summary>
        /// <param name="parameterExpression">Parameter used in the final lambda expression.</param>
        /// <param name="memberExpression">Member expression for the mapped filter field.</param>
        /// <param name="filterTerm">Parsed filter term.</param>
        /// <param name="typedValues">Typed values converted for the member type.</param>
        /// <param name="dataForCustomMethods">Optional extra data for custom providers.</param>
        public PaginationFilterExpressionContext(
            ParameterExpression parameterExpression,
            Expression memberExpression,
            PaginationFilterTerm filterTerm,
            IReadOnlyList<object?> typedValues,
            object[]? dataForCustomMethods)
        {
            ParameterExpression = parameterExpression ?? throw new ArgumentNullException(nameof(parameterExpression));
            MemberExpression = memberExpression ?? throw new ArgumentNullException(nameof(memberExpression));
            FilterTerm = filterTerm ?? throw new ArgumentNullException(nameof(filterTerm));
            TypedValues = typedValues ?? throw new ArgumentNullException(nameof(typedValues));
            DataForCustomMethods = dataForCustomMethods;
        }

        /// <summary>
        /// Gets the parameter expression used for the entity in the lambda.
        /// </summary>
        public ParameterExpression ParameterExpression { get; }

        /// <summary>
        /// Gets the target member expression for filtering.
        /// </summary>
        public Expression MemberExpression { get; }

        /// <summary>
        /// Gets the parsed filter term.
        /// </summary>
        public PaginationFilterTerm FilterTerm { get; }

        /// <summary>
        /// Gets filter values converted to the member type.
        /// </summary>
        public IReadOnlyList<object?> TypedValues { get; }

        /// <summary>
        /// Gets optional extra data for custom expression providers.
        /// </summary>
        public object[]? DataForCustomMethods { get; }
    }
}
