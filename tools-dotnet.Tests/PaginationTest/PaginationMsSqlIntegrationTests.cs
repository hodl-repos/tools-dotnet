using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Testcontainers.MsSql;
using tools_dotnet.Pagination.Attributes;
using tools_dotnet.Pagination.Models;
using tools_dotnet.Pagination.Services;

namespace tools_dotnet.Tests.PaginationTest
{
    [TestFixture]
    public class PaginationMsSqlIntegrationTests
    {
        private static readonly PaginationProcessor Processor = new(
            filterExpressionProviders: [new SqlServerPaginationFilterExpressionProvider()]
        );

        private static readonly IReadOnlyList<int> AllIds = [1, 2, 3, 4, 5];
        private static readonly Guid Guid1 = Guid.Parse("11111111-1111-1111-1111-111111111111");
        private static readonly Guid Guid2 = Guid.Parse("22222222-2222-2222-2222-222222222222");
        private static readonly Guid Guid3 = Guid.Parse("33333333-3333-3333-3333-333333333333");
        private static readonly Guid Guid4 = Guid.Parse("44444444-4444-4444-4444-444444444444");
        private static readonly Guid Guid5 = Guid.Parse("55555555-5555-5555-5555-555555555555");

        private readonly MsSqlContainer _msSqlContainer = new MsSqlBuilder(
            "mcr.microsoft.com/mssql/server:2022-latest"
        )
            .WithPassword("Your_strong_password123!")
            .WithCleanUp(true)
            .Build();

        private DbContextOptions<PaginationMsSqlTestDbContext> _dbContextOptions = null!;

        [OneTimeSetUp]
        public async Task BeforeAllAsync()
        {
            await _msSqlContainer.StartAsync();
        }

        [OneTimeTearDown]
        public async Task AfterAllAsync()
        {
            await _msSqlContainer.DisposeAsync();
        }

        [SetUp]
        public async Task SetupRun()
        {
            _dbContextOptions = CreateDbContextOptions(_msSqlContainer.GetConnectionString());

            await using (var dbContext = new PaginationMsSqlTestDbContext(_dbContextOptions))
            {
                await dbContext.Database.EnsureCreatedAsync();

                dbContext.Entities.AddRange(
                    new PaginationMsSqlEntity
                    {
                        Id = 1,
                        Name = "Milk",
                        IntValue = 10,
                        LongValue = 100,
                        ExternalId = Guid1,
                    },
                    new PaginationMsSqlEntity
                    {
                        Id = 2,
                        Name = "MILKY",
                        IntValue = 20,
                        LongValue = 200,
                        ExternalId = Guid2,
                    },
                    new PaginationMsSqlEntity
                    {
                        Id = 3,
                        Name = "almond milk",
                        IntValue = 30,
                        LongValue = 300,
                        ExternalId = Guid3,
                    },
                    new PaginationMsSqlEntity
                    {
                        Id = 4,
                        Name = "Bread",
                        IntValue = 40,
                        LongValue = 400,
                        ExternalId = Guid4,
                    },
                    new PaginationMsSqlEntity
                    {
                        Id = 5,
                        Name = null,
                        IntValue = 50,
                        LongValue = 500,
                        ExternalId = Guid5,
                    }
                );

                await dbContext.SaveChangesAsync();
            }
        }

        public static IEnumerable<TestCaseData> StringOperatorCases()
        {
            yield return new TestCaseData($"name{PaginationOperator.Equal.Id}Milk", new[] { 1 });
            yield return new TestCaseData(
                $"name{PaginationOperator.EqualCaseInsensitive.Id}milk",
                new[] { 1 }
            );
            yield return new TestCaseData(
                $"name{PaginationOperator.NotEquals.Id}Milk",
                new[] { 2, 3, 4, 5 }
            );
            yield return new TestCaseData(
                $"name{PaginationOperator.NotEqualsCaseInsensitive.Id}milk",
                new[] { 2, 3, 4, 5 }
            );
            yield return new TestCaseData(
                $"name{PaginationOperator.Contains.Id}milk",
                new[] { 1, 2, 3 }
            );
            yield return new TestCaseData(
                $"name{PaginationOperator.ContainsCaseInsensitive.Id}milk",
                new[] { 1, 2, 3 }
            );
            yield return new TestCaseData(
                $"name{PaginationOperator.NotContains.Id}milk",
                new[] { 4, 5 }
            );
            yield return new TestCaseData(
                $"name{PaginationOperator.NotContainsCaseInsensitive.Id}milk",
                new[] { 4, 5 }
            );
            yield return new TestCaseData(
                $"name{PaginationOperator.StartsWith.Id}Mi",
                new[] { 1, 2 }
            );
            yield return new TestCaseData(
                $"name{PaginationOperator.StartsWithCaseInsensitive.Id}mi",
                new[] { 1, 2 }
            );
            yield return new TestCaseData(
                $"name{PaginationOperator.NotStartsWith.Id}Mi",
                new[] { 3, 4, 5 }
            );
            yield return new TestCaseData(
                $"name{PaginationOperator.NotStartsWithCaseInsensitive.Id}mi",
                new[] { 3, 4, 5 }
            );
            yield return new TestCaseData(
                $"name{PaginationOperator.EndsWith.Id}ilk",
                new[] { 1, 3 }
            );
            yield return new TestCaseData(
                $"name{PaginationOperator.EndsWithCaseInsensitive.Id}ilk",
                new[] { 1, 3 }
            );
            yield return new TestCaseData(
                $"name{PaginationOperator.NotEndsWith.Id}ilk",
                new[] { 2, 4, 5 }
            );
            yield return new TestCaseData(
                $"name{PaginationOperator.NotEndsWithCaseInsensitive.Id}ilk",
                new[] { 2, 4, 5 }
            );
        }

        public static IEnumerable<TestCaseData> IntOperatorCases()
        {
            foreach (var op in PaginationOperator.Values.OrderBy(x => x.Id, StringComparer.Ordinal))
            {
                yield return new TestCaseData(
                    $"int_value{op.Id}20",
                    ResolveExpectedIdsForIntOperator(op)
                );
            }
        }

        public static IEnumerable<TestCaseData> LongOperatorCases()
        {
            foreach (var op in PaginationOperator.Values.OrderBy(x => x.Id, StringComparer.Ordinal))
            {
                yield return new TestCaseData(
                    $"long_value{op.Id}200",
                    ResolveExpectedIdsForLongOperator(op)
                );
            }
        }

        public static IEnumerable<TestCaseData> GuidOperatorCases()
        {
            foreach (var op in PaginationOperator.Values.OrderBy(x => x.Id, StringComparer.Ordinal))
            {
                yield return new TestCaseData(
                    $"external_id{op.Id}{Guid2}",
                    ResolveExpectedIdsForGuidOperator(op)
                );
            }
        }

        [TestCaseSource(nameof(StringOperatorCases))]
        public async Task Apply_ShouldSupportAllStringOperators(string filters, int[] expectedIds)
        {
            var result = await ApplyFilterAsync(filters);
            result.ShouldBe(expectedIds);
        }

        [TestCaseSource(nameof(IntOperatorCases))]
        public async Task Apply_ShouldSupportAllIntOperators(string filters, int[] expectedIds)
        {
            var result = await ApplyFilterAsync(filters);
            result.ShouldBe(expectedIds);
        }

        [TestCaseSource(nameof(LongOperatorCases))]
        public async Task Apply_ShouldSupportAllLongOperators(string filters, int[] expectedIds)
        {
            var result = await ApplyFilterAsync(filters);
            result.ShouldBe(expectedIds);
        }

        [TestCaseSource(nameof(GuidOperatorCases))]
        public async Task Apply_ShouldSupportAllGuidOperators(string filters, int[] expectedIds)
        {
            var result = await ApplyFilterAsync(filters);
            result.ShouldBe(expectedIds);
        }

        private async Task<IReadOnlyList<int>> ApplyFilterAsync(string filters)
        {
            await using var dbContext = new PaginationMsSqlTestDbContext(_dbContextOptions);
            var model = new PaginationModel { Filters = filters };

            var query = Processor.Apply(
                model,
                dbContext.Entities.AsNoTracking(),
                applySorting: false,
                applyPagination: false
            );

            return await query.OrderBy(x => x.Id).Select(x => x.Id).ToArrayAsync();
        }

        private static int[] ResolveExpectedIdsForIntOperator(PaginationOperator op)
        {
            if (op == PaginationOperator.Equal)
            {
                return [2];
            }

            if (op == PaginationOperator.NotEquals)
            {
                return [1, 3, 4, 5];
            }

            if (op == PaginationOperator.GreaterThan)
            {
                return [3, 4, 5];
            }

            if (op == PaginationOperator.GreaterThanOrEqual)
            {
                return [2, 3, 4, 5];
            }

            if (op == PaginationOperator.LessThan)
            {
                return [1];
            }

            if (op == PaginationOperator.LessThanOrEqual)
            {
                return [1, 2];
            }

            return [.. AllIds];
        }

        private static int[] ResolveExpectedIdsForLongOperator(PaginationOperator op)
        {
            if (op == PaginationOperator.Equal)
            {
                return [2];
            }

            if (op == PaginationOperator.NotEquals)
            {
                return [1, 3, 4, 5];
            }

            if (op == PaginationOperator.GreaterThan)
            {
                return [3, 4, 5];
            }

            if (op == PaginationOperator.GreaterThanOrEqual)
            {
                return [2, 3, 4, 5];
            }

            if (op == PaginationOperator.LessThan)
            {
                return [1];
            }

            if (op == PaginationOperator.LessThanOrEqual)
            {
                return [1, 2];
            }

            return [.. AllIds];
        }

        private static int[] ResolveExpectedIdsForGuidOperator(PaginationOperator op)
        {
            if (op == PaginationOperator.Equal)
            {
                return [2];
            }

            if (op == PaginationOperator.NotEquals)
            {
                return [1, 3, 4, 5];
            }

            if (op == PaginationOperator.GreaterThan)
            {
                return [3, 4, 5];
            }

            if (op == PaginationOperator.GreaterThanOrEqual)
            {
                return [2, 3, 4, 5];
            }

            if (op == PaginationOperator.LessThan)
            {
                return [1];
            }

            if (op == PaginationOperator.LessThanOrEqual)
            {
                return [1, 2];
            }

            return [.. AllIds];
        }

        private static DbContextOptions<PaginationMsSqlTestDbContext> CreateDbContextOptions(
            string containerConnectionString
        )
        {
            var builder = new SqlConnectionStringBuilder(containerConnectionString)
            {
                InitialCatalog = $"test_{Guid.NewGuid():N}",
            };

            return new DbContextOptionsBuilder<PaginationMsSqlTestDbContext>()
                .UseSqlServer(builder.ConnectionString)
                .Options;
        }

        private sealed class PaginationMsSqlTestDbContext : DbContext
        {
            public PaginationMsSqlTestDbContext(
                DbContextOptions<PaginationMsSqlTestDbContext> options
            )
                : base(options) { }

            public DbSet<PaginationMsSqlEntity> Entities => Set<PaginationMsSqlEntity>();

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                modelBuilder.Entity<PaginationMsSqlEntity>(entity =>
                {
                    entity.ToTable("pagination_mssql_entities");
                    entity.HasKey(x => x.Id);
                    entity.Property(x => x.Id).ValueGeneratedNever();
                    entity.Property(x => x.Name).HasColumnName("name");
                    entity.Property(x => x.IntValue).HasColumnName("int_value");
                    entity.Property(x => x.LongValue).HasColumnName("long_value");
                    entity.Property(x => x.ExternalId).HasColumnName("external_id");
                });
            }
        }

        private sealed class PaginationMsSqlEntity
        {
            [Pagination(Name = "id", CanFilter = false, CanSort = false)]
            public int Id { get; init; }

            [Pagination(Name = "name", CanFilter = true, CanSort = false)]
            public string? Name { get; init; }

            [Pagination(Name = "int_value", CanFilter = true, CanSort = false)]
            public int IntValue { get; init; }

            [Pagination(Name = "long_value", CanFilter = true, CanSort = false)]
            public long LongValue { get; init; }

            [Pagination(Name = "external_id", CanFilter = true, CanSort = false)]
            public Guid ExternalId { get; init; }
        }
    }
}

