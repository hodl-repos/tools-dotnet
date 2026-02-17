using tools_dotnet.Pagination.Models;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace tools_dotnet.Pagination.Services
{
    public sealed class PaginationFilterExpressionContext
    {
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

        public ParameterExpression ParameterExpression { get; }

        public Expression MemberExpression { get; }

        public PaginationFilterTerm FilterTerm { get; }

        public IReadOnlyList<object?> TypedValues { get; }

        public object[]? DataForCustomMethods { get; }
    }
}
