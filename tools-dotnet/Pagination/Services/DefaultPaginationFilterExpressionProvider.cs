using tools_dotnet.Pagination.Models;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace tools_dotnet.Pagination.Services
{
    public class DefaultPaginationFilterExpressionProvider : IPaginationFilterExpressionProvider
    {
        private static readonly MethodInfo ContainsMethod = typeof(string).GetMethod(nameof(string.Contains), new[] { typeof(string) })!;
        private static readonly MethodInfo StartsWithMethod = typeof(string).GetMethod(nameof(string.StartsWith), new[] { typeof(string) })!;
        private static readonly MethodInfo EndsWithMethod = typeof(string).GetMethod(nameof(string.EndsWith), new[] { typeof(string) })!;
        private static readonly MethodInfo ToUpperMethod = typeof(string).GetMethod(nameof(string.ToUpper), Type.EmptyTypes)!;

        public bool TryBuildExpression(PaginationFilterExpressionContext context, out Expression? expression)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            expression = null;

            if (context.TypedValues.Count == 0)
            {
                return false;
            }

            var partialExpressions = new List<Expression>();

            foreach (var typedValue in context.TypedValues)
            {
                var singleExpression = BuildSingleExpression(context.MemberExpression, context.FilterTerm.Operator, typedValue);

                if (singleExpression != null)
                {
                    partialExpressions.Add(singleExpression);
                }
            }

            if (partialExpressions.Count == 0)
            {
                return false;
            }

            expression = partialExpressions[0];

            for (var i = 1; i < partialExpressions.Count; i++)
            {
                expression = context.FilterTerm.Operator.IsNegated
                    ? Expression.AndAlso(expression, partialExpressions[i])
                    : Expression.OrElse(expression, partialExpressions[i]);
            }

            return true;
        }

        private static Expression? BuildSingleExpression(Expression memberExpression, PaginationOperator @operator, object? typedValue)
        {
            if (@operator.IsStringOperator)
            {
                return BuildStringExpression(memberExpression, @operator, typedValue);
            }

            return BuildComparableExpression(memberExpression, @operator, typedValue);
        }

        private static Expression? BuildStringExpression(Expression memberExpression, PaginationOperator @operator, object? typedValue)
        {
            if (memberExpression.Type != typeof(string) || typedValue is not string stringValue)
            {
                return null;
            }

            var nullExpression = Expression.Constant(null, typeof(string));
            var notNullExpression = Expression.NotEqual(memberExpression, nullExpression);
            var valueExpression = Expression.Constant(
                @operator.IsCaseInsensitive ? stringValue.ToUpperInvariant() : stringValue,
                typeof(string));
            var normalizedMemberExpression = @operator.IsCaseInsensitive
                ? Expression.Call(memberExpression, ToUpperMethod)
                : memberExpression;

            Expression positive;

            if (@operator == PaginationOperator.EqualCaseInsensitive || @operator == PaginationOperator.NotEqualsCaseInsensitive)
            {
                positive = Expression.AndAlso(notNullExpression, Expression.Equal(normalizedMemberExpression, valueExpression));
            }
            else if (@operator == PaginationOperator.Contains ||
                     @operator == PaginationOperator.NotContains ||
                     @operator == PaginationOperator.ContainsCaseInsensitive ||
                     @operator == PaginationOperator.NotContainsCaseInsensitive)
            {
                var containsExpression = Expression.Call(normalizedMemberExpression, ContainsMethod, valueExpression);
                positive = Expression.AndAlso(notNullExpression, containsExpression);
            }
            else if (@operator == PaginationOperator.StartsWith ||
                     @operator == PaginationOperator.NotStartsWith ||
                     @operator == PaginationOperator.StartsWithCaseInsensitive ||
                     @operator == PaginationOperator.NotStartsWithCaseInsensitive)
            {
                var startsWithExpression = Expression.Call(normalizedMemberExpression, StartsWithMethod, valueExpression);
                positive = Expression.AndAlso(notNullExpression, startsWithExpression);
            }
            else if (@operator == PaginationOperator.EndsWith ||
                     @operator == PaginationOperator.NotEndsWith ||
                     @operator == PaginationOperator.EndsWithCaseInsensitive ||
                     @operator == PaginationOperator.NotEndsWithCaseInsensitive)
            {
                var endsWithExpression = Expression.Call(normalizedMemberExpression, EndsWithMethod, valueExpression);
                positive = Expression.AndAlso(notNullExpression, endsWithExpression);
            }
            else
            {
                return null;
            }

            if (@operator.IsNegated)
            {
                return Expression.Not(positive);
            }

            return positive;
        }

        private static Expression? BuildComparableExpression(Expression memberExpression, PaginationOperator @operator, object? typedValue)
        {
            if (typedValue == null && @operator != PaginationOperator.Equal && @operator != PaginationOperator.NotEquals)
            {
                return null;
            }

            var constantExpression = CreateTypedConstant(memberExpression.Type, typedValue);

            try
            {
                if (@operator == PaginationOperator.Equal)
                {
                    return Expression.Equal(memberExpression, constantExpression);
                }

                if (@operator == PaginationOperator.NotEquals)
                {
                    return Expression.NotEqual(memberExpression, constantExpression);
                }

                if (@operator == PaginationOperator.GreaterThan)
                {
                    return Expression.GreaterThan(memberExpression, constantExpression);
                }

                if (@operator == PaginationOperator.GreaterThanOrEqual)
                {
                    return Expression.GreaterThanOrEqual(memberExpression, constantExpression);
                }

                if (@operator == PaginationOperator.LessThan)
                {
                    return Expression.LessThan(memberExpression, constantExpression);
                }

                if (@operator == PaginationOperator.LessThanOrEqual)
                {
                    return Expression.LessThanOrEqual(memberExpression, constantExpression);
                }
            }
            catch (InvalidOperationException)
            {
                return null;
            }
            catch (ArgumentException)
            {
                return null;
            }

            return null;
        }

        private static Expression CreateTypedConstant(Type targetType, object? value)
        {
            if (value == null)
            {
                return Expression.Constant(null, targetType);
            }

            var nullableType = Nullable.GetUnderlyingType(targetType);

            if (nullableType == null)
            {
                return Expression.Constant(value, targetType);
            }

            return Expression.Convert(Expression.Constant(value, nullableType), targetType);
        }
    }
}
