using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using tools_dotnet.Pagination.Attributes;
using tools_dotnet.Pagination.Models;

namespace tools_dotnet.Pagination.Builders
{
    /// <summary>
    /// Builds pagination filter strings with type-safe member selectors.
    /// </summary>
    /// <typeparam name="TEntity">Entity or DTO type containing filterable members.</typeparam>
    public sealed class PaginationFilterBuilder<TEntity>
    {
        private static readonly HashSet<char> EscapableCharacters = new()
        {
            ',',
            '|',
            '\\',
            '(',
            ')',
            '!',
            '@',
            '_',
            '=',
            '>',
            '<',
            '*',
        };

        private readonly List<string> _terms = new();

        /// <summary>
        /// Adds one AND filter term parsed from a simple binary expression.
        /// </summary>
        public PaginationFilterBuilder<TEntity> And(
            Expression<Func<TEntity, bool>> predicate
        )
        {
            if (predicate == null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }

            var (field, value, op) = ParseBinaryPredicate(predicate.Body);
            return AddTerm([field], [FormatValue(value)], op);
        }

        /// <summary>
        /// Adds one AND filter term.
        /// </summary>
        public PaginationFilterBuilder<TEntity> And<TValue>(
            Expression<Func<TEntity, TValue>> field,
            TValue value,
            PaginationOperator op
        )
        {
            return AddTerm([ResolveFieldPath(field)], [FormatValue(value)], op);
        }

        /// <summary>
        /// Adds one AND filter term with multiple OR values.
        /// </summary>
        public PaginationFilterBuilder<TEntity> AndValues<TValue>(
            Expression<Func<TEntity, TValue>> field,
            IEnumerable<TValue> values,
            PaginationOperator op
        )
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            return AddTerm([ResolveFieldPath(field)], values.Select(FormatValue), op);
        }

        /// <summary>
        /// Adds one AND filter term with multiple OR fields.
        /// </summary>
        public PaginationFilterBuilder<TEntity> AndAny<TValue>(
            IReadOnlyList<Expression<Func<TEntity, TValue>>> fields,
            TValue value,
            PaginationOperator op
        )
        {
            return AddTerm(ResolveFieldPaths(fields), [FormatValue(value)], op);
        }

        /// <summary>
        /// Adds one AND filter term with multiple OR fields and multiple OR values.
        /// </summary>
        public PaginationFilterBuilder<TEntity> AndAnyValues<TValue>(
            IReadOnlyList<Expression<Func<TEntity, TValue>>> fields,
            IEnumerable<TValue> values,
            PaginationOperator op
        )
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            return AddTerm(ResolveFieldPaths(fields), values.Select(FormatValue), op);
        }

        /// <summary>
        /// Builds the raw pagination filters string.
        /// </summary>
        public string Build()
        {
            return string.Join(",", _terms);
        }

        /// <summary>
        /// Builds a pagination model containing the generated filters string.
        /// </summary>
        public PaginationModel ToPaginationModel(int? page = null, int? pageSize = null)
        {
            return new PaginationModel
            {
                Filters = Build(),
                Page = page,
                PageSize = pageSize,
            };
        }

        private PaginationFilterBuilder<TEntity> AddTerm(
            IReadOnlyList<string> fields,
            IEnumerable<string> values,
            PaginationOperator op
        )
        {
            if (fields == null)
            {
                throw new ArgumentNullException(nameof(fields));
            }

            if (fields.Count == 0)
            {
                throw new ArgumentException("At least one field must be provided.", nameof(fields));
            }

            if (fields.Any(string.IsNullOrWhiteSpace))
            {
                throw new ArgumentException("Fields cannot be null or whitespace.", nameof(fields));
            }

            if (op == null)
            {
                throw new ArgumentNullException(nameof(op));
            }

            var valueList = values?.ToArray() ?? throw new ArgumentNullException(nameof(values));

            if (valueList.Length == 0)
            {
                throw new ArgumentException(
                    "At least one value must be provided.",
                    nameof(values)
                );
            }

            var fieldSegment = fields.Count == 1 ? fields[0] : $"({string.Join("|", fields)})";
            _terms.Add($"{fieldSegment}{op.Id}{string.Join("|", valueList)}");

            return this;
        }

        private static IReadOnlyList<string> ResolveFieldPaths<TValue>(
            IReadOnlyList<Expression<Func<TEntity, TValue>>> fields
        )
        {
            if (fields == null)
            {
                throw new ArgumentNullException(nameof(fields));
            }

            if (fields.Count == 0)
            {
                throw new ArgumentException("At least one field must be provided.", nameof(fields));
            }

            return fields.Select(ResolveFieldPath).ToArray();
        }

        private static string ResolveFieldPath<TValue>(
            Expression<Func<TEntity, TValue>> field
        )
        {
            if (field == null)
            {
                throw new ArgumentNullException(nameof(field));
            }

            return ResolveFieldPath(UnwrapConvert(field.Body));
        }

        private static string ResolveFieldPath(Expression expression)
        {
            var members = new Stack<MemberInfo>();
            var current = UnwrapConvert(expression);

            while (current is MemberExpression memberExpression)
            {
                members.Push(memberExpression.Member);
                current = UnwrapConvert(memberExpression.Expression);
            }

            if (current is not ParameterExpression parameterExpression
                || parameterExpression.Type != typeof(TEntity)
                || members.Count == 0)
            {
                throw new ArgumentException(
                    "Expression must select a member path from the filter entity.",
                    nameof(expression)
                );
            }

            return string.Join(".", members.Select(GetPaginationMemberName));
        }

        private static string GetPaginationMemberName(MemberInfo member)
        {
            if (
                member.MemberType != MemberTypes.Property
                && member.MemberType != MemberTypes.Field
            )
            {
                throw new ArgumentException("Expression must select a property or field.");
            }

            var attribute = member.GetCustomAttribute<PaginationAttribute>();

            return string.IsNullOrWhiteSpace(attribute?.Name) ? member.Name : attribute!.Name!;
        }

        private static (string Field, object? Value, PaginationOperator Operator) ParseBinaryPredicate(
            Expression expression
        )
        {
            var body = UnwrapConvert(expression);

            if (body is not BinaryExpression binaryExpression)
            {
                throw new ArgumentException(
                    "Predicate must be a simple binary comparison expression."
                );
            }

            if (TryResolveMemberAndValue(binaryExpression.Left, binaryExpression.Right, out var field, out var value))
            {
                return (field, value, ResolveOperator(binaryExpression.NodeType));
            }

            if (TryResolveMemberAndValue(binaryExpression.Right, binaryExpression.Left, out field, out value))
            {
                return (field, value, ReverseOperator(ResolveOperator(binaryExpression.NodeType)));
            }

            throw new ArgumentException(
                "Predicate must compare one entity member path with a constant or captured value."
            );
        }

        private static bool TryResolveMemberAndValue(
            Expression maybeMember,
            Expression maybeValue,
            out string field,
            out object? value
        )
        {
            try
            {
                field = ResolveFieldPath(maybeMember);
            }
            catch (ArgumentException)
            {
                field = string.Empty;
                value = null;
                return false;
            }

            value = Evaluate(maybeValue);
            return true;
        }

        private static PaginationOperator ResolveOperator(ExpressionType expressionType)
        {
            return expressionType switch
            {
                ExpressionType.Equal => PaginationOperator.Equal,
                ExpressionType.NotEqual => PaginationOperator.NotEquals,
                ExpressionType.GreaterThan => PaginationOperator.GreaterThan,
                ExpressionType.GreaterThanOrEqual => PaginationOperator.GreaterThanOrEqual,
                ExpressionType.LessThan => PaginationOperator.LessThan,
                ExpressionType.LessThanOrEqual => PaginationOperator.LessThanOrEqual,
                _ => throw new ArgumentException(
                    $"Expression operator '{expressionType}' is not supported."
                ),
            };
        }

        private static PaginationOperator ReverseOperator(PaginationOperator op)
        {
            if (op == PaginationOperator.GreaterThan)
            {
                return PaginationOperator.LessThan;
            }

            if (op == PaginationOperator.GreaterThanOrEqual)
            {
                return PaginationOperator.LessThanOrEqual;
            }

            if (op == PaginationOperator.LessThan)
            {
                return PaginationOperator.GreaterThan;
            }

            if (op == PaginationOperator.LessThanOrEqual)
            {
                return PaginationOperator.GreaterThanOrEqual;
            }

            return op;
        }

        private static object? Evaluate(Expression expression)
        {
            expression = UnwrapConvert(expression);

            if (ReferencesFilterEntity(expression))
            {
                throw new ArgumentException(
                    "Predicate values cannot reference the filter entity.",
                    nameof(expression)
                );
            }

            if (expression is ConstantExpression constantExpression)
            {
                return constantExpression.Value;
            }

            var lambda = Expression.Lambda<Func<object?>>(
                Expression.Convert(expression, typeof(object))
            );

            return lambda.Compile().Invoke();
        }

        private static bool ReferencesFilterEntity(Expression expression)
        {
            var visitor = new FilterEntityReferenceVisitor();
            visitor.Visit(expression);
            return visitor.Found;
        }

        private static Expression UnwrapConvert(Expression? expression)
        {
            while (
                expression is UnaryExpression unaryExpression
                && (
                    unaryExpression.NodeType == ExpressionType.Convert
                    || unaryExpression.NodeType == ExpressionType.ConvertChecked
                )
            )
            {
                expression = unaryExpression.Operand;
            }

            if (expression == null)
            {
                throw new ArgumentException("Expression cannot be null.");
            }

            return expression;
        }

        private static string FormatValue<TValue>(TValue value)
        {
            if (value == null)
            {
                return "null";
            }

            if (value is string stringValue)
            {
                if (string.Equals(stringValue, "null", StringComparison.OrdinalIgnoreCase))
                {
                    return "\\null";
                }

                return EscapeValue(stringValue);
            }

            if (value is DateTimeOffset dateTimeOffset)
            {
                return EscapeValue(dateTimeOffset.ToString("O", CultureInfo.InvariantCulture));
            }

            if (value is DateTime dateTime)
            {
                return EscapeValue(dateTime.ToString("O", CultureInfo.InvariantCulture));
            }

            if (value is DateOnly dateOnly)
            {
                return EscapeValue(dateOnly.ToString("O", CultureInfo.InvariantCulture));
            }

            if (value is TimeOnly timeOnly)
            {
                return EscapeValue(timeOnly.ToString("O", CultureInfo.InvariantCulture));
            }

            if (value is bool boolean)
            {
                return boolean ? "true" : "false";
            }

            if (value is IFormattable formattable)
            {
                return EscapeValue(formattable.ToString(null, CultureInfo.InvariantCulture));
            }

            return EscapeValue(value.ToString() ?? string.Empty);
        }

        private static string EscapeValue(string value)
        {
            if (value.Length == 0)
            {
                return value;
            }

            var escaped = new System.Text.StringBuilder(value.Length);

            foreach (var c in value)
            {
                if (EscapableCharacters.Contains(c))
                {
                    escaped.Append('\\');
                }

                escaped.Append(c);
            }

            return escaped.ToString();
        }

        private sealed class FilterEntityReferenceVisitor : ExpressionVisitor
        {
            public bool Found { get; private set; }

            protected override Expression VisitParameter(ParameterExpression node)
            {
                if (node.Type == typeof(TEntity))
                {
                    Found = true;
                }

                return node;
            }
        }
    }
}
