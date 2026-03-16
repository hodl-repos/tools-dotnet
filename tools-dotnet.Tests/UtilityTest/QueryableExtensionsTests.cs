using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using tools_dotnet.Pagination.Models;
using tools_dotnet.Pagination.Services;
using tools_dotnet.Paging.Impl;
using tools_dotnet.Utility;

namespace tools_dotnet.Tests.UtilityTest
{
    [TestFixture]
    public class QueryableExtensionsTests
    {
        [Test]
        public void SortFilterAndPage_ShouldDeserializeOnce_WhenProcessorSupportsDeserializedModel()
        {
            var processor = new OptimizedPaginationProcessor();
            var query = Enumerable.Range(1, 5)
                .Select(id => new QueryableEntity { Id = id, Name = $"item-{id}" })
                .AsQueryable();
            var pagination = new ApiPagination { Page = 2, PageSize = 2 };

            var result = query.SortFilterAndPage(pagination, processor);

            result.Items.Select(x => x.Id).ShouldBe([3, 4]);
            result.Metadata.TotalItemCount.ShouldBe(5);
            processor.DeserializeCallCount.ShouldBe(1);
            processor.DeserializedApplyCallCount.ShouldBe(2);
        }

        [Test]
        public async Task SortFilterAndPageAsync_ShouldDeserializeOnce_WhenProcessorSupportsDeserializedModel()
        {
            var processor = new OptimizedPaginationProcessor();
            var pagination = new ApiPagination { Page = 2, PageSize = 2 };

            await using var dbContext = CreateDbContext();
            await SeedAsync(dbContext);

            var result = await dbContext.Entities.SortFilterAndPageAsync(pagination, processor);

            result.Items.Select(x => x.Id).ShouldBe([3, 4]);
            result.Metadata.TotalItemCount.ShouldBe(5);
            processor.DeserializeCallCount.ShouldBe(1);
            processor.DeserializedApplyCallCount.ShouldBe(2);
        }

        [Test]
        public async Task SortFilterAndPageWithProjectToAsync_ShouldDeserializeOnce_WhenProcessorSupportsDeserializedModel()
        {
            var processor = new OptimizedPaginationProcessor();
            var pagination = new ApiPagination { Page = 2, PageSize = 2 };
            var mapper = CreateMapper();

            await using var dbContext = CreateDbContext();
            await SeedAsync(dbContext);

            var result = await dbContext.Entities.SortFilterAndPageWithProjectToAsync<QueryableEntity, QueryableDto>(
                pagination,
                processor,
                mapper
            );

            result.Items.Select(x => x.Id).ShouldBe([3, 4]);
            result.Metadata.TotalItemCount.ShouldBe(5);
            processor.DeserializeCallCount.ShouldBe(1);
            processor.DeserializedApplyCallCount.ShouldBe(2);
        }

        [Test]
        public async Task SortFilterAndPageAsync_ShouldFallbackToPublicApply_WhenProcessorDoesNotSupportDeserializedModel()
        {
            var processor = new LegacyPaginationProcessor();
            var pagination = new ApiPagination { Page = 2, PageSize = 2 };

            await using var dbContext = CreateDbContext();
            await SeedAsync(dbContext);

            var result = await dbContext.Entities.SortFilterAndPageAsync(pagination, processor);

            result.Items.Select(x => x.Id).ShouldBe([3, 4]);
            result.Metadata.TotalItemCount.ShouldBe(5);
            processor.ApplyCallCount.ShouldBe(2);
        }

        private static QueryableExtensionsDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<QueryableExtensionsDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
                .Options;

            return new QueryableExtensionsDbContext(options);
        }

        private static async Task SeedAsync(QueryableExtensionsDbContext dbContext)
        {
            dbContext.Entities.AddRange(
                Enumerable.Range(1, 5).Select(id => new QueryableEntity { Id = id, Name = $"item-{id}" })
            );

            await dbContext.SaveChangesAsync();
        }

        private static IMapper CreateMapper()
        {
            var mapperConfiguration = new MapperConfiguration(
                config => config.CreateMap<QueryableEntity, QueryableDto>(),
                NullLoggerFactory.Instance
            );

            return mapperConfiguration.CreateMapper();
        }

        private sealed class QueryableExtensionsDbContext : DbContext
        {
            public QueryableExtensionsDbContext(DbContextOptions<QueryableExtensionsDbContext> options)
                : base(options) { }

            public DbSet<QueryableEntity> Entities => Set<QueryableEntity>();
        }

        private sealed class QueryableEntity
        {
            public int Id { get; set; }

            public string Name { get; set; } = string.Empty;
        }

        private sealed class QueryableDto
        {
            public int Id { get; set; }

            public string Name { get; set; } = string.Empty;
        }

        private sealed class OptimizedPaginationProcessor
            : IPaginationProcessor, IDeserializedPaginationProcessor
        {
            public int DeserializeCallCount { get; private set; }

            public int DeserializedApplyCallCount { get; private set; }

            public IQueryable<TEntity> Apply<TEntity>(
                PaginationModel model,
                IQueryable<TEntity> source,
                object[]? dataForCustomMethods = null,
                bool applyFiltering = true,
                bool applySorting = true,
                bool applyPagination = true
            )
            {
                throw new InvalidOperationException("Raw Apply should not be called.");
            }

            public DeserializedPaginationModel Deserialize(PaginationModel model)
            {
                DeserializeCallCount++;

                return new DeserializedPaginationModel(
                    [],
                    [],
                    model.Page ?? 1,
                    model.PageSize ?? 25
                );
            }

            public IQueryable<TEntity> Apply<TEntity>(
                DeserializedPaginationModel model,
                IQueryable<TEntity> source,
                object[]? dataForCustomMethods = null,
                bool applyFiltering = true,
                bool applySorting = true,
                bool applyPagination = true
            )
            {
                DeserializedApplyCallCount++;

                if (!applyPagination)
                {
                    return source;
                }

                return source.Skip((model.Page - 1) * model.PageSize).Take(model.PageSize);
            }
        }

        private sealed class LegacyPaginationProcessor : IPaginationProcessor
        {
            public int ApplyCallCount { get; private set; }

            public IQueryable<TEntity> Apply<TEntity>(
                PaginationModel model,
                IQueryable<TEntity> source,
                object[]? dataForCustomMethods = null,
                bool applyFiltering = true,
                bool applySorting = true,
                bool applyPagination = true
            )
            {
                ApplyCallCount++;

                if (!applyPagination)
                {
                    return source;
                }

                var page = model.Page ?? 1;
                var pageSize = model.PageSize ?? 25;

                return source.Skip((page - 1) * pageSize).Take(pageSize);
            }
        }
    }
}
