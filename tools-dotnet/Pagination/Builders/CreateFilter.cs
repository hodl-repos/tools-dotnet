using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using tools_dotnet.Pagination.Models;

namespace tools_dotnet.Pagination.Builders
{
    /// <summary>
    /// Entry point for type-safe pagination filter construction.
    /// </summary>
    /// <typeparam name="TEntity">Entity or DTO type containing filterable members.</typeparam>
    public static class CreateFilter<TEntity>
    {
        /// <summary>
        /// Creates a builder with a filter term parsed from a simple binary expression.
        /// </summary>
        public static PaginationFilterBuilder<TEntity> And(
            Expression<Func<TEntity, bool>> predicate
        )
        {
            return new PaginationFilterBuilder<TEntity>().And(predicate);
        }

        /// <summary>
        /// Creates a builder with one filter term.
        /// </summary>
        public static PaginationFilterBuilder<TEntity> And<TValue>(
            Expression<Func<TEntity, TValue>> field,
            TValue value,
            PaginationOperator op
        )
        {
            return new PaginationFilterBuilder<TEntity>().And(field, value, op);
        }

        /// <summary>
        /// Creates a builder with one multi-value filter term.
        /// </summary>
        public static PaginationFilterBuilder<TEntity> AndValues<TValue>(
            Expression<Func<TEntity, TValue>> field,
            IEnumerable<TValue> values,
            PaginationOperator op
        )
        {
            return new PaginationFilterBuilder<TEntity>().AndValues(field, values, op);
        }

        /// <summary>
        /// Creates a builder with one grouped-field OR filter term.
        /// </summary>
        public static PaginationFilterBuilder<TEntity> AndAny<TValue>(
            IReadOnlyList<Expression<Func<TEntity, TValue>>> fields,
            TValue value,
            PaginationOperator op
        )
        {
            return new PaginationFilterBuilder<TEntity>().AndAny(fields, value, op);
        }

        /// <summary>
        /// Creates a builder with one grouped-field OR and multi-value filter term.
        /// </summary>
        public static PaginationFilterBuilder<TEntity> AndAnyValues<TValue>(
            IReadOnlyList<Expression<Func<TEntity, TValue>>> fields,
            IEnumerable<TValue> values,
            PaginationOperator op
        )
        {
            return new PaginationFilterBuilder<TEntity>().AndAnyValues(fields, values, op);
        }
    }
}
