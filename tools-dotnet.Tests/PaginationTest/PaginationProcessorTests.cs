using Shouldly;
using tools_dotnet.Pagination.Attributes;
using tools_dotnet.Pagination.Models;
using tools_dotnet.Pagination.Services;

namespace tools_dotnet.Tests.PaginationTest
{
    [TestFixture]
    public class PaginationProcessorTests
    {
        [Test]
        public void Apply_ShouldFilterSortAndPaginate()
        {
            var processor = new PaginationProcessor();
            var model = new PaginationModel
            {
                Filters = "name@=a,age>=18",
                Sorts = "-age",
                Page = 1,
                PageSize = 2
            };

            var source = new List<TestEntity>
            {
                new() { Name = "Anna", Age = 20 },
                new() { Name = "Bob", Age = 44 },
                new() { Name = "Clara", Age = 30 },
                new() { Name = "Dave", Age = 19 }
            }.AsQueryable();

            var result = processor.Apply(model, source).ToList();

            result.Count.ShouldBe(2);
            result[0].Name.ShouldBe("Clara");
            result[1].Name.ShouldBe("Anna");
        }

        [Test]
        public void Apply_ShouldHandleNegatedMultiValueFilter()
        {
            var processor = new PaginationProcessor();
            var model = new PaginationModel
            {
                Filters = "name!=Anna|Bob"
            };

            var source = new List<TestEntity>
            {
                new() { Name = "Anna", Age = 20 },
                new() { Name = "Bob", Age = 44 },
                new() { Name = "Clara", Age = 30 }
            }.AsQueryable();

            var result = processor.Apply(model, source, applySorting: false, applyPagination: false).ToList();

            result.Count.ShouldBe(1);
            result[0].Name.ShouldBe("Clara");
        }

        [Test]
        public void Apply_ShouldResolveAliasAndRespectCanFilter()
        {
            var processor = new PaginationProcessor();
            var model = new PaginationModel
            {
                Filters = "display_name==Anna,hidden==top"
            };

            var source = new List<AliasedEntity>
            {
                new() { Name = "Anna", Hidden = "blocked" },
                new() { Name = "Anna", Hidden = "top" }
            }.AsQueryable();

            var result = processor.Apply(model, source, applySorting: false, applyPagination: false).ToList();

            result.Count.ShouldBe(2);
        }

        private sealed class TestEntity
        {
            public string? Name { get; init; }

            public int Age { get; init; }
        }

        private sealed class AliasedEntity
        {
            [Pagination(Name = "display_name", CanFilter = true)]
            public string? Name { get; init; }

            [Pagination(Name = "hidden", CanFilter = false)]
            public string? Hidden { get; init; }
        }
    }
}
