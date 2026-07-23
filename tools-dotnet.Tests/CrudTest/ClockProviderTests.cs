using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using tools_dotnet.Dao.Crud;
using tools_dotnet.Dao.Crud.Impl;
using tools_dotnet.Dao.Entity;
using tools_dotnet.Dao.Interceptors;
using tools_dotnet.Dao.KeyWrapper;
using tools_dotnet.Pagination.Services;
using tools_dotnet.Time;

namespace tools_dotnet.Tests.CrudTest
{
    [TestFixture]
    public class ClockProviderTests
    {
        private IMapper _mapper = null!;
        private PaginationProcessor _paginationProcessor = null!;

        [SetUp]
        public void Setup()
        {
            var mapperConfiguration = new MapperConfiguration(
                config => config.CreateMap<ClockEntity, ClockEntity>(),
                NullLoggerFactory.Instance
            );

            _mapper = mapperConfiguration.CreateMapper();
            _paginationProcessor = new PaginationProcessor();
        }

        [Test]
        public async Task TimestampsInterceptor_ShouldUseClockProvider_ForCreatedAndUpdatedTimestamps()
        {
            var now = new DateTimeOffset(2030, 1, 2, 3, 4, 5, TimeSpan.Zero);
            var clockProvider = new FixedClockProvider(now);
            var options = CreateDbContextOptions(clockProvider);

            await using var dbContext = new ClockDbContext(options);
            var entity = new ClockEntity { Id = 1, Name = "created" };

            dbContext.ClockEntities.Add(entity);
            await dbContext.SaveChangesAsync();

            entity.CreatedTimestamp.ShouldBe(now);
            entity.UpdatedTimestamp.ShouldBe(now);
        }

        [Test]
        public void TimestampsInterceptor_ShouldUseClockProvider_ForSynchronousSaveChanges()
        {
            var now = new DateTimeOffset(2030, 2, 3, 4, 5, 6, TimeSpan.Zero);
            var options = CreateDbContextOptions(new FixedClockProvider(now));

            using var dbContext = new ClockDbContext(options);
            var entity = new ClockEntity { Id = 1, Name = "created synchronously" };

            dbContext.ClockEntities.Add(entity);
            dbContext.SaveChanges();

            entity.CreatedTimestamp.ShouldBe(now);
            entity.UpdatedTimestamp.ShouldBe(now);
        }

        [Test]
        public async Task TimestampsInterceptor_ShouldAdvanceUpdatedTimestamp_AndPreserveCreatedTimestamp()
        {
            var createdAt = new DateTimeOffset(2030, 3, 4, 5, 6, 7, TimeSpan.Zero);
            var updatedAt = new DateTimeOffset(2030, 3, 5, 6, 7, 8, TimeSpan.Zero);
            var clockProvider = new MutableClockProvider(createdAt);
            var options = CreateDbContextOptions(clockProvider);

            await using var dbContext = new ClockDbContext(options);
            var entity = new ClockEntity { Id = 1, Name = "created" };
            dbContext.ClockEntities.Add(entity);
            await dbContext.SaveChangesAsync();

            clockProvider.UtcNow = updatedAt;
            entity.Name = "updated";
            entity.CreatedTimestamp = updatedAt;
            await dbContext.SaveChangesAsync();

            entity.CreatedTimestamp.ShouldBe(createdAt);
            entity.UpdatedTimestamp.ShouldBe(updatedAt);
        }

        [Test]
        public async Task TimestampsInterceptor_ShouldUseSystemClockProvider_WhenNoneIsSupplied()
        {
            var options = new DbContextOptionsBuilder<ClockDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
                .AddInterceptors(new TimestampsInterceptor())
                .Options;

            await using var dbContext = new ClockDbContext(options);
            var entity = new ClockEntity { Id = 1, Name = "system clock" };

            dbContext.ClockEntities.Add(entity);
            await dbContext.SaveChangesAsync();

            entity.CreatedTimestamp.ShouldNotBe(default);
            entity.CreatedTimestamp.Offset.ShouldBe(TimeSpan.Zero);
            entity.UpdatedTimestamp.ShouldBe(entity.CreatedTimestamp);
        }

        public static IEnumerable<TestCaseData> RepositoryDeleteCases()
        {
            yield return new TestCaseData(
                RepositoryKind.Basic,
                new DateTimeOffset(2031, 2, 3, 4, 5, 6, TimeSpan.Zero)
            );
            yield return new TestCaseData(
                RepositoryKind.Concurrent,
                new DateTimeOffset(2032, 3, 4, 5, 6, 7, TimeSpan.Zero)
            );
            yield return new TestCaseData(
                RepositoryKind.KeyWrapped,
                new DateTimeOffset(2033, 4, 5, 6, 7, 8, TimeSpan.Zero)
            );
            yield return new TestCaseData(
                RepositoryKind.ConcurrentKeyWrapped,
                new DateTimeOffset(2034, 5, 6, 7, 8, 9, TimeSpan.Zero)
            );
        }

        [TestCaseSource(nameof(RepositoryDeleteCases))]
        public async Task CrudRepository_ShouldUseClockProvider_ForDeletedTimestamp(
            RepositoryKind repositoryKind,
            DateTimeOffset deletedAt
        )
        {
            var seed = await SeedAsync();

            await using (var dbContext = new ClockDbContext(seed.Options))
            {
                var clockProvider = new FixedClockProvider(deletedAt);
                await RemoveUsingRepositoryAsync(
                    repositoryKind,
                    dbContext,
                    seed.Entity,
                    clockProvider
                );
            }

            var deleted = await LoadAsync(seed.Options, seed.Entity.Id);
            deleted.DeletedTimestamp.ShouldBe(deletedAt);
        }

        private async Task RemoveUsingRepositoryAsync(
            RepositoryKind repositoryKind,
            ClockDbContext dbContext,
            ClockEntity entity,
            IClockProvider clockProvider
        )
        {
            switch (repositoryKind)
            {
                case RepositoryKind.Basic:
                    await new ClockEntityRepo(
                        dbContext,
                        _mapper,
                        _paginationProcessor,
                        clockProvider
                    ).RemoveAsync(entity.Id);
                    return;
                case RepositoryKind.Concurrent:
                    await new ConcurrentClockEntityRepo(
                        dbContext,
                        _mapper,
                        _paginationProcessor,
                        clockProvider
                    ).RemoveAsync(entity.Id, entity.UpdatedTimestamp);
                    return;
                case RepositoryKind.KeyWrapped:
                    await new KeyWrappedClockEntityRepo(
                        dbContext,
                        _mapper,
                        _paginationProcessor,
                        clockProvider
                    ).RemoveAsync(new ClockEntityKeyWrapper(entity.Id));
                    return;
                case RepositoryKind.ConcurrentKeyWrapped:
                    await new ConcurrentKeyWrappedClockEntityRepo(
                        dbContext,
                        _mapper,
                        _paginationProcessor,
                        clockProvider
                    ).RemoveAsync(
                        new ClockEntityKeyWrapper(entity.Id),
                        entity.UpdatedTimestamp
                    );
                    return;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(repositoryKind),
                        repositoryKind,
                        "Unknown repository kind."
                    );
            }
        }

        private static async Task<SeedResult> SeedAsync()
        {
            var seedClockProvider = new FixedClockProvider(
                new DateTimeOffset(2029, 12, 31, 23, 59, 58, TimeSpan.Zero)
            );
            var options = CreateDbContextOptions(seedClockProvider);
            var entity = new ClockEntity { Id = 1, Name = "seed" };

            await using var dbContext = new ClockDbContext(options);
            dbContext.ClockEntities.Add(entity);
            await dbContext.SaveChangesAsync();

            return new SeedResult(options, entity);
        }

        private static async Task<ClockEntity> LoadAsync(
            DbContextOptions<ClockDbContext> options,
            int id
        )
        {
            await using var dbContext = new ClockDbContext(options);
            return await dbContext.ClockEntities.SingleAsync(x => x.Id == id);
        }

        private static DbContextOptions<ClockDbContext> CreateDbContextOptions(
            IClockProvider clockProvider
        )
        {
            return new DbContextOptionsBuilder<ClockDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
                .AddInterceptors(new TimestampsInterceptor(clockProvider))
                .Options;
        }

        private sealed record SeedResult(
            DbContextOptions<ClockDbContext> Options,
            ClockEntity Entity
        );

        public enum RepositoryKind
        {
            Basic,
            Concurrent,
            KeyWrapped,
            ConcurrentKeyWrapped,
        }

        private sealed class FixedClockProvider : IClockProvider
        {
            public FixedClockProvider(DateTimeOffset utcNow)
            {
                UtcNow = utcNow;
            }

            public DateTimeOffset UtcNow { get; }
        }

        private sealed class MutableClockProvider : IClockProvider
        {
            public MutableClockProvider(DateTimeOffset utcNow)
            {
                UtcNow = utcNow;
            }

            public DateTimeOffset UtcNow { get; set; }
        }

        private sealed class ClockDbContext : DbContext
        {
            public ClockDbContext(DbContextOptions<ClockDbContext> options)
                : base(options) { }

            public DbSet<ClockEntity> ClockEntities => Set<ClockEntity>();
        }

        private sealed class ClockEntity
            : IEntityWithId<int>,
                IChangeTrackingEntity,
                IAuditableEntity
        {
            public int Id { get; set; }

            public string Name { get; set; } = string.Empty;

            public DateTimeOffset CreatedTimestamp { get; set; }

            public DateTimeOffset? UpdatedTimestamp { get; set; }

            public DateTimeOffset? DeletedTimestamp { get; set; }
        }

        private sealed class ClockEntityKeyWrapper : IKeyWrapper<ClockEntity>
        {
            private readonly int _id;

            public ClockEntityKeyWrapper(int id)
            {
                _id = id;
            }

            public string[] GetKeyAsString()
            {
                return [_id.ToString()];
            }

            public void UpdateEntityWithContainingResource(ClockEntity entity) { }

            public void UpdateKeyWrapperByEntity(ClockEntity entity) { }

            public System.Linq.Expressions.Expression<Func<ClockEntity, bool>> GetKeyFilter()
            {
                return entity => entity.Id == _id;
            }

            public System.Linq.Expressions.Expression<Func<ClockEntity, bool>> GetContainingResourceFilter()
            {
                return _ => true;
            }
        }

        private sealed class ClockEntityRepo : BaseCrudRepo<ClockEntity, int>
        {
            public ClockEntityRepo(
                DbContext dbContext,
                IMapper mapper,
                IPaginationProcessor paginationProcessor,
                IClockProvider clockProvider
            )
                : base(dbContext, mapper, paginationProcessor, clockProvider) { }
        }

        private sealed class ConcurrentClockEntityRepo
            : BaseConcurrentCrudRepo<ClockEntity, int, DateTimeOffset?>
        {
            public ConcurrentClockEntityRepo(
                DbContext dbContext,
                IMapper mapper,
                IPaginationProcessor paginationProcessor,
                IClockProvider clockProvider
            )
                : base(
                    dbContext,
                    mapper,
                    paginationProcessor,
                    CrudConcurrencyConfiguration.UpdatedTimestamp(),
                    clockProvider
                )
            { }
        }

        private sealed class KeyWrappedClockEntityRepo
            : BaseCrudRepoWithKeyWrapper<ClockEntity, ClockEntityKeyWrapper>
        {
            public KeyWrappedClockEntityRepo(
                DbContext dbContext,
                IMapper mapper,
                IPaginationProcessor paginationProcessor,
                IClockProvider clockProvider
            )
                : base(dbContext, mapper, paginationProcessor, clockProvider) { }
        }

        private sealed class ConcurrentKeyWrappedClockEntityRepo
            : BaseConcurrentCrudRepoWithKeyWrapper<
                ClockEntity,
                ClockEntityKeyWrapper,
                DateTimeOffset?
            >
        {
            public ConcurrentKeyWrappedClockEntityRepo(
                DbContext dbContext,
                IMapper mapper,
                IPaginationProcessor paginationProcessor,
                IClockProvider clockProvider
            )
                : base(
                    dbContext,
                    mapper,
                    paginationProcessor,
                    CrudConcurrencyConfiguration.UpdatedTimestamp(),
                    clockProvider
                )
            { }
        }
    }
}
