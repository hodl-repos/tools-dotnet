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
            var model = new PaginationModel { Filters = "name!=Anna|Bob" };

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

        [Test]
        public void Apply_ShouldUseAscendingDefaultSort_WhenNoSortIsRequested()
        {
            var processor = new PaginationProcessor();
            var model = new PaginationModel();

            var source = new List<DefaultSortEntity>
            {
                new() { Id = 1, Name = "Clara", Age = 30 },
                new() { Id = 2, Name = "Anna", Age = 20 },
                new() { Id = 3, Name = "Bob", Age = 40 }
            }.AsQueryable();

            var result = processor.Apply(model, source, applyFiltering: false, applyPagination: false)
                .Select(x => x.Id)
                .ToList();

            result.ShouldBe([2, 3, 1]);
        }

        [Test]
        public void Apply_ShouldUseDescendingDefaultSort_WhenNoSortIsRequested()
        {
            var processor = new PaginationProcessor();
            var model = new PaginationModel();

            var source = new List<DescendingDefaultSortEntity>
            {
                new() { Id = 1, Age = 30 },
                new() { Id = 2, Age = 20 },
                new() { Id = 3, Age = 40 }
            }.AsQueryable();

            var result = processor.Apply(model, source, applyFiltering: false, applyPagination: false)
                .Select(x => x.Id)
                .ToList();

            result.ShouldBe([3, 1, 2]);
        }

        [Test]
        public void Apply_ShouldPreferRequestedSort_OverDefaultSort()
        {
            var processor = new PaginationProcessor();
            var model = new PaginationModel
            {
                Sorts = "age"
            };

            var source = new List<DefaultSortEntity>
            {
                new() { Id = 1, Name = "Clara", Age = 30 },
                new() { Id = 2, Name = "Anna", Age = 20 },
                new() { Id = 3, Name = "Bob", Age = 40 }
            }.AsQueryable();

            var result = processor.Apply(model, source, applyFiltering: false, applyPagination: false)
                .Select(x => x.Id)
                .ToList();

            result.ShouldBe([2, 1, 3]);
        }

        [Test]
        public void Apply_ShouldIgnoreDefaultSort_WhenCanSortIsFalse()
        {
            var processor = new PaginationProcessor();
            var model = new PaginationModel();

            var source = new List<BlockedDefaultSortEntity>
            {
                new() { Id = 1, Name = "Clara" },
                new() { Id = 2, Name = "Anna" },
                new() { Id = 3, Name = "Bob" }
            }.AsQueryable();

            var result = processor.Apply(model, source, applyFiltering: false, applyPagination: false)
                .Select(x => x.Id)
                .ToList();

            result.ShouldBe([1, 2, 3]);
        }

        [Test]
        public void Apply_ShouldUseNestedDefaultSort_WhenParentAllowsSortSubProperties()
        {
            var processor = new PaginationProcessor();
            var model = new PaginationModel();

            var source = new List<NestedDefaultSortEntity>
            {
                new() { Id = 1, Profile = new NestedDefaultSortProfile { Rank = 30 } },
                new() { Id = 2, Profile = new NestedDefaultSortProfile { Rank = 10 } },
                new() { Id = 3, Profile = new NestedDefaultSortProfile { Rank = 20 } }
            }.AsQueryable();

            var result = processor.Apply(model, source, applyFiltering: false, applyPagination: false)
                .Select(x => x.Id)
                .ToList();

            result.ShouldBe([2, 3, 1]);
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

        private sealed class DefaultSortEntity
        {
            public int Id { get; init; }

            [Pagination(Name = "name", CanFilter = true, CanSort = true, IsDefaultSorted = true)]
            public string Name { get; init; } = string.Empty;

            [Pagination(Name = "age", CanFilter = true, CanSort = true)]
            public int Age { get; init; }
        }

        private sealed class DescendingDefaultSortEntity
        {
            public int Id { get; init; }

            [Pagination(
                Name = "age",
                CanFilter = true,
                CanSort = true,
                IsDefaultSorted = true,
                DefaultSortDescending = true)]
            public int Age { get; init; }
        }

        private sealed class BlockedDefaultSortEntity
        {
            public int Id { get; init; }

            [Pagination(Name = "name", CanFilter = true, CanSort = false, IsDefaultSorted = true)]
            public string Name { get; init; } = string.Empty;
        }

        private sealed class NestedDefaultSortEntity
        {
            public int Id { get; init; }

            [Pagination(
                Name = "profile",
                CanFilter = false,
                CanSort = false,
                CanSortSubProperties = true)]
            public NestedDefaultSortProfile Profile { get; init; } = new();
        }

        private sealed class NestedDefaultSortProfile
        {
            [Pagination(Name = "rank", CanFilter = true, CanSort = true, IsDefaultSorted = true)]
            public int Rank { get; init; }
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
