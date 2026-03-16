using Shouldly;
using tools_dotnet.Pagination.Models;
using tools_dotnet.Pagination.Services;
using System.Linq.Expressions;

namespace tools_dotnet.Tests.PaginationTest
{
    [TestFixture]
    public class DefaultPaginationFilterExpressionProviderTests
    {
        [Test]
        public void TryBuildExpression_ShouldUseToUpper_ByDefault()
        {
            var provider = new DefaultPaginationFilterExpressionProvider();
            var context = CreateContext(PaginationOperator.EqualCaseInsensitive, "milk");

            provider.TryBuildExpression(context, out var expression).ShouldBeTrue();
            expression.ShouldNotBeNull();
            ContainsStringMethodCall(expression!, nameof(string.ToUpper)).ShouldBeTrue();
            ContainsStringMethodCall(expression!, nameof(string.ToLower)).ShouldBeFalse();
        }

        [Test]
        public void TryBuildExpression_ShouldUseToLower_WhenConfigured()
        {
            var provider = new DefaultPaginationFilterExpressionProvider(PaginationCaseInsensitiveNormalization.ToLower);
            var context = CreateContext(PaginationOperator.EqualCaseInsensitive, "milk");

            provider.TryBuildExpression(context, out var expression).ShouldBeTrue();
            expression.ShouldNotBeNull();
            ContainsStringMethodCall(expression!, nameof(string.ToLower)).ShouldBeTrue();
            ContainsStringMethodCall(expression!, nameof(string.ToUpper)).ShouldBeFalse();
        }

        [Test]
        public void TryBuildExpression_ShouldAvoidNormalization_WhenConfiguredAsNone()
        {
            var provider = new DefaultPaginationFilterExpressionProvider(PaginationCaseInsensitiveNormalization.None);
            var context = CreateContext(PaginationOperator.EqualCaseInsensitive, "milk");

            provider.TryBuildExpression(context, out var expression).ShouldBeTrue();
            expression.ShouldNotBeNull();
            ContainsStringMethodCall(expression!, nameof(string.ToLower)).ShouldBeFalse();
            ContainsStringMethodCall(expression!, nameof(string.ToUpper)).ShouldBeFalse();
        }

        [Test]
        public void Apply_ShouldWorkWithPostgreSqlProviderDropIn()
        {
            var processor = new PaginationProcessor(filterExpressionProviders: [new PostgreSqlPaginationFilterExpressionProvider()]);
            var model = new PaginationModel
            {
                Filters = "name==*milk"
            };

            var source = new List<ProviderEntity>
            {
                new() { Id = 1, Name = "MILK" },
                new() { Id = 2, Name = "milk" },
                new() { Id = 3, Name = "bread" }
            }.AsQueryable();

            var result = processor.Apply(model, source, applySorting: false, applyPagination: false).Select(x => x.Id).ToList();

            result.ShouldBe(new[] { 1, 2 });
        }

        [Test]
        public void Apply_ShouldWorkWithSqlServerProviderDropIn()
        {
            var processor = new PaginationProcessor(filterExpressionProviders: [new SqlServerPaginationFilterExpressionProvider()]);
            var model = new PaginationModel
            {
                Filters = "name==*milk"
            };

            var source = new List<ProviderEntity>
            {
                new() { Id = 1, Name = "MILK" },
                new() { Id = 2, Name = "milk" },
                new() { Id = 3, Name = "bread" }
            }.AsQueryable();

            var result = processor.Apply(model, source, applySorting: false, applyPagination: false).Select(x => x.Id).ToList();

            result.ShouldBe(new[] { 1, 2 });
        }

        private static PaginationFilterExpressionContext CreateContext(PaginationOperator @operator, string value)
        {
            var parameterExpression = Expression.Parameter(typeof(ProviderEntity), "entity");
            var memberExpression = Expression.Property(parameterExpression, nameof(ProviderEntity.Name));
            var filterTerm = new PaginationFilterTerm(
                [nameof(ProviderEntity.Name)],
                @operator,
                [value]);

            return new PaginationFilterExpressionContext(
                parameterExpression,
                memberExpression,
                filterTerm,
                [value],
                dataForCustomMethods: null);
        }

        private static bool ContainsStringMethodCall(Expression expression, string methodName)
        {
            var visitor = new MethodCallFinder(methodName);
            visitor.Visit(expression);
            return visitor.Found;
        }

        private sealed class MethodCallFinder : ExpressionVisitor
        {
            private readonly string _methodName;

            public MethodCallFinder(string methodName)
            {
                _methodName = methodName;
            }

            public bool Found { get; private set; }

            protected override Expression VisitMethodCall(MethodCallExpression node)
            {
                if (node.Method.DeclaringType == typeof(string) &&
                    string.Equals(node.Method.Name, _methodName, StringComparison.Ordinal))
                {
                    Found = true;
                }

                return base.VisitMethodCall(node);
            }
        }

        private sealed class ProviderEntity
        {
            public int Id { get; init; }

            public string Name { get; init; } = string.Empty;
        }
    }
}