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

        [Test]
        public void Apply_ShouldUseCustomFilterMethod_WhenNoMemberMatches()
        {
            var processor = new PaginationProcessor(customFilterMethods: [new TestCustomFilterMethods()]);
            var model = new PaginationModel
            {
                Filters = "is_adult==21"
            };

            var source = new List<CustomFilterEntity>
            {
                new() { Name = "Anna", Age = 20 },
                new() { Name = "Bob", Age = 21 },
                new() { Name = "Clara", Age = 31 }
            }.AsQueryable();

            var result = processor.Apply(model, source, applySorting: false, applyPagination: false).ToList();

            result.Count.ShouldBe(2);
            result.Select(x => x.Name).ShouldBe(["Bob", "Clara"]);
        }

        [Test]
        public void Apply_ShouldUseGenericCustomFilterMethod_WhenConstraintMatches()
        {
            var processor = new PaginationProcessor(customFilterMethods: [new TestCustomFilterMethods()]);
            var model = new PaginationModel
            {
                Filters = "by_name==clara"
            };

            var source = new List<CustomFilterEntity>
            {
                new() { Name = "Anna", Age = 20 },
                new() { Name = "Bob", Age = 21 },
                new() { Name = "Clara", Age = 31 }
            }.AsQueryable();

            var result = processor.Apply(model, source, applySorting: false, applyPagination: false).ToList();

            result.Count.ShouldBe(1);
            result[0].Name.ShouldBe("Clara");
        }

        [Test]
        public void Apply_ShouldAllowNestedFiltering_WhenParentAllowsSubProperties()
        {
            var processor = new PaginationProcessor();
            var model = new PaginationModel
            {
                Filters = "profile.display_name==alice"
            };

            var source = new List<NestedFilterEntity>
            {
                new() { Id = 1, Profile = new NestedProfile { DisplayName = "alice" } },
                new() { Id = 2, Profile = new NestedProfile { DisplayName = "bob" } }
            }.AsQueryable();

            var result = processor.Apply(model, source, applySorting: false, applyPagination: false).ToList();

            result.Select(x => x.Id).ShouldBe([1]);
        }

        [Test]
        public void Apply_ShouldIgnoreNestedFiltering_WhenParentDisallowsSubProperties()
        {
            var processor = new PaginationProcessor();
            var model = new PaginationModel
            {
                Filters = "blocked_profile.display_name==alice"
            };

            var source = new List<NestedFilterEntity>
            {
                new() { Id = 1, BlockedProfile = new NestedProfile { DisplayName = "alice" } },
                new() { Id = 2, BlockedProfile = new NestedProfile { DisplayName = "bob" } }
            }.AsQueryable();

            var result = processor.Apply(model, source, applySorting: false, applyPagination: false).ToList();

            result.Select(x => x.Id).ShouldBe([1, 2]);
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

        private sealed class NestedFilterEntity
        {
            public int Id { get; init; }

            [Pagination(Name = "profile", CanFilter = false, CanSort = false, CanFilterSubProperties = true, CanSortSubProperties = true)]
            public NestedProfile Profile { get; init; } = new();

            [Pagination(Name = "blocked_profile", CanFilter = false, CanSort = false, CanFilterSubProperties = false, CanSortSubProperties = false)]
            public NestedProfile BlockedProfile { get; init; } = new();
        }

        private sealed class NestedProfile
        {
            [Pagination(Name = "display_name", CanFilter = true, CanSort = true)]
            public string? DisplayName { get; init; }
        }

        private interface ICustomNamedEntity
        {
            string Name { get; }
        }

        private sealed class CustomFilterEntity : ICustomNamedEntity
        {
            public string Name { get; init; } = string.Empty;

            public int Age { get; init; }
        }

        private sealed class TestCustomFilterMethods : IPaginationCustomFilterMethods
        {
            public IQueryable<CustomFilterEntity> Is_Adult(IQueryable<CustomFilterEntity> source, string op, string[] values)
            {
                if (!string.Equals(op, PaginationOperator.Equal.Id, StringComparison.Ordinal) || values.Length == 0)
                {
                    return source;
                }

                if (!int.TryParse(values[0], out var minimumAge))
                {
                    return source;
                }

                return source.Where(x => x.Age >= minimumAge);
            }

            public IQueryable<TEntity> By_Name<TEntity>(IQueryable<TEntity> source, string op, string[] values)
                where TEntity : ICustomNamedEntity
            {
                if (!string.Equals(op, PaginationOperator.Equal.Id, StringComparison.Ordinal) || values.Length == 0)
                {
                    return source;
                }

                var expectedName = values[0];
                return source.Where(x => x.Name.Equals(expectedName, StringComparison.OrdinalIgnoreCase));
            }
        }
    }
}