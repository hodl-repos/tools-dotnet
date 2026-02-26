using Shouldly;
using tools_dotnet.Pagination.Attributes;
using tools_dotnet.Pagination.Models;
using tools_dotnet.Pagination.Services;

namespace tools_dotnet.Tests.PaginationTest
{
    [TestFixture]
    public class PaginationCustomSortMethodsTests
    {
        [Test]
        public void Apply_ShouldUseCustomSortMethod_WhenNoMemberMatches()
        {
            var processor = new PaginationProcessor(customSortMethods: [new TestCustomSortMethods()]);
            var model = new PaginationModel
            {
                Sorts = "by_name_length"
            };

            var source = new List<CustomSortEntity>
            {
                new() { Id = 1, Name = "Bobby", Age = 30 },
                new() { Id = 2, Name = "Al", Age = 30 },
                new() { Id = 3, Name = "Cara", Age = 30 }
            }.AsQueryable();

            var result = processor.Apply(model, source, applyFiltering: false, applyPagination: false).Select(x => x.Id).ToList();

            result.ShouldBe([2, 3, 1]);
        }

        [Test]
        public void Apply_ShouldUseCustomSortMethod_AsThenBy_WhenPreviousSortExists()
        {
            var processor = new PaginationProcessor(customSortMethods: [new TestCustomSortMethods()]);
            var model = new PaginationModel
            {
                Sorts = "age,by_name_length"
            };

            var source = new List<CustomSortEntity>
            {
                new() { Id = 1, Name = "Bobby", Age = 20 },
                new() { Id = 2, Name = "Al", Age = 20 },
                new() { Id = 3, Name = "Cara", Age = 30 }
            }.AsQueryable();

            var result = processor.Apply(model, source, applyFiltering: false, applyPagination: false).Select(x => x.Id).ToList();

            result.ShouldBe([2, 1, 3]);
        }

        [Test]
        public void Apply_ShouldUseGenericCustomSortMethod_WhenConstraintMatches()
        {
            var processor = new PaginationProcessor(customSortMethods: [new TestCustomSortMethods()]);
            var model = new PaginationModel
            {
                Sorts = "-by_name"
            };

            var source = new List<CustomSortEntity>
            {
                new() { Id = 1, Name = "Alpha", Age = 30 },
                new() { Id = 2, Name = "Beta", Age = 30 },
                new() { Id = 3, Name = "Gamma", Age = 30 }
            }.AsQueryable();

            var result = processor.Apply(model, source, applyFiltering: false, applyPagination: false).Select(x => x.Id).ToList();

            result.ShouldBe([3, 2, 1]);
        }

        private interface ICustomNamedEntity
        {
            string Name { get; }
        }

        private sealed class CustomSortEntity : ICustomNamedEntity
        {
            public int Id { get; init; }

            public string Name { get; init; } = string.Empty;

            [Pagination(Name = "age", CanFilter = true, CanSort = true)]
            public int Age { get; init; }
        }

        private sealed class TestCustomSortMethods : IPaginationCustomSortsMethods
        {
            public IQueryable<CustomSortEntity> by_name_length(IQueryable<CustomSortEntity> source, bool useThenBy, bool desc)
            {
                if (useThenBy)
                {
                    var orderedSource = (IOrderedQueryable<CustomSortEntity>)source;
                    return desc
                        ? orderedSource.ThenByDescending(x => x.Name.Length)
                        : orderedSource.ThenBy(x => x.Name.Length);
                }

                return desc
                    ? source.OrderByDescending(x => x.Name.Length)
                    : source.OrderBy(x => x.Name.Length);
            }

            public IQueryable<TEntity> by_name<TEntity>(IQueryable<TEntity> source, bool useThenBy, bool desc)
                where TEntity : ICustomNamedEntity
            {
                if (useThenBy)
                {
                    var orderedSource = (IOrderedQueryable<TEntity>)source;
                    return desc
                        ? orderedSource.ThenByDescending(x => x.Name)
                        : orderedSource.ThenBy(x => x.Name);
                }

                return desc
                    ? source.OrderByDescending(x => x.Name)
                    : source.OrderBy(x => x.Name);
            }
        }
    }
}
