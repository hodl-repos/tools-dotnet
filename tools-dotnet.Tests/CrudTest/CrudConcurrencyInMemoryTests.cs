using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using tools_dotnet.Dao.Crud;
using tools_dotnet.Dao.Crud.Impl;
using tools_dotnet.Dao.Entity;
using tools_dotnet.Dao.Interceptors;
using tools_dotnet.Dao.KeyWrapper;
using tools_dotnet.Dto;
using tools_dotnet.Exceptions;
using tools_dotnet.Pagination.Services;

namespace tools_dotnet.Tests.CrudTest
{
    [TestFixture]
    public class CrudConcurrencyInMemoryTests
    {
        private DbContextOptions<CrudConcurrencyInMemoryDbContext> _dbContextOptions = null!;
        private IMapper _mapper = null!;
        private PaginationProcessor _paginationProcessor = null!;

        [SetUp]
        public void Setup()
        {
            _dbContextOptions = new DbContextOptionsBuilder<CrudConcurrencyInMemoryDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
                .AddInterceptors(new TimestampsInterceptor())
                .Options;

            var mapperConfiguration = new MapperConfiguration(
                config =>
                {
                    config.CreateMap<TrackedEntity, TrackedEntity>();
                    config.CreateMap<TrackedEntity, TrackedEntityDto>().ReverseMap();
                    config.CreateMap<TrackedEntityInputDto, TrackedEntity>();
                    config.CreateMap<LegacyTrackedEntityInputDto, TrackedEntity>();
                    config.CreateMap<KeyWrappedEntity, KeyWrappedEntity>();
                    config.CreateMap<KeyWrappedEntity, KeyWrappedEntityDto>().ReverseMap();
                    config.CreateMap<KeyWrappedEntityInputDto, KeyWrappedEntity>();
                },
                NullLoggerFactory.Instance
            );

            _mapper = mapperConfiguration.CreateMapper();
            _paginationProcessor = new PaginationProcessor();
        }

        [Test]
        public async Task UpdateAsync_ShouldAdvanceUpdatedTimestamp_WhenEntityTimestampMatches()
        {
            var seeded = await AddTrackedEntityAsync();
            await Task.Delay(10);

            await using var dbContext = CreateDbContext();
            var repo = new TrackedEntityRepo(dbContext, _mapper, _paginationProcessor);

            await repo.UpdateAsync(
                new TrackedEntity
                {
                    Id = seeded.Id,
                    Name = "updated",
                    CreatedTimestamp = seeded.CreatedTimestamp,
                    UpdatedTimestamp = seeded.UpdatedTimestamp
                }
            );

            var updated = await LoadTrackedEntityAsync(seeded.Id);
            updated.Name.ShouldBe("updated");
            updated.UpdatedTimestamp.ShouldNotBeNull();
            updated.UpdatedTimestamp.ShouldNotBe(seeded.UpdatedTimestamp);
        }

        [Test]
        public async Task UpdateAsync_ShouldPreserveCreatedTimestamp_ForLegacyEntityRepo()
        {
            var seeded = await AddTrackedEntityAsync();
            var forgedCreatedTimestamp = seeded.CreatedTimestamp.AddDays(7);
            await Task.Delay(10);

            await using var dbContext = CreateDbContext();
            var repo = new LegacyTrackedEntityRepo(dbContext, _mapper, _paginationProcessor);

            await repo.UpdateAsync(
                new TrackedEntity
                {
                    Id = seeded.Id,
                    Name = "updated",
                    CreatedTimestamp = forgedCreatedTimestamp,
                    UpdatedTimestamp = seeded.UpdatedTimestamp
                }
            );

            var updated = await LoadTrackedEntityAsync(seeded.Id);
            updated.Name.ShouldBe("updated");
            updated.CreatedTimestamp.ShouldBe(seeded.CreatedTimestamp);
        }

        [Test]
        public async Task UpdateAsync_ShouldPreserveCreatedTimestamp_ForLegacyDtoRepo()
        {
            var seeded = await AddTrackedEntityAsync();
            var forgedCreatedTimestamp = seeded.CreatedTimestamp.AddDays(7);
            await Task.Delay(10);

            await using var dbContext = CreateDbContext();
            var repo = new LegacyTrackedEntityDtoRepo(dbContext, _mapper, _paginationProcessor);

            await repo.UpdateAsync(
                new TrackedEntityInputDto
                {
                    Id = seeded.Id,
                    Name = "updated",
                    CreatedTimestamp = forgedCreatedTimestamp,
                    UpdatedTimestamp = seeded.UpdatedTimestamp
                }
            );

            var updated = await LoadTrackedEntityAsync(seeded.Id);
            updated.Name.ShouldBe("updated");
            updated.CreatedTimestamp.ShouldBe(seeded.CreatedTimestamp);
        }

        [Test]
        public async Task UpdateAsync_ShouldPreserveCreatedTimestamp_ForLegacyKeyWrapperRepo()
        {
            var seeded = await AddKeyWrappedEntityAsync();
            var forgedCreatedTimestamp = seeded.CreatedTimestamp.AddDays(7);
            var keyWrapper = new ScopedKeyWrapper(seeded.ParentId, seeded.Id);
            await Task.Delay(10);

            await using var dbContext = CreateDbContext();
            var repo = new LegacyKeyWrappedEntityRepo(dbContext, _mapper, _paginationProcessor);

            await repo.UpdateAsync(
                keyWrapper,
                new KeyWrappedEntity
                {
                    Id = seeded.Id,
                    ParentId = seeded.ParentId,
                    Name = "updated",
                    CreatedTimestamp = forgedCreatedTimestamp,
                    UpdatedTimestamp = seeded.UpdatedTimestamp
                }
            );

            var updated = await LoadKeyWrappedEntityAsync(seeded.ParentId, seeded.Id);
            updated.Name.ShouldBe("updated");
            updated.CreatedTimestamp.ShouldBe(seeded.CreatedTimestamp);
        }

        [Test]
        public async Task UpdateAsync_ShouldPreserveCreatedTimestamp_ForLegacyKeyWrapperDtoRepo()
        {
            var seeded = await AddKeyWrappedEntityAsync();
            var forgedCreatedTimestamp = seeded.CreatedTimestamp.AddDays(7);
            var keyWrapper = new ScopedKeyWrapper(seeded.ParentId, seeded.Id);
            await Task.Delay(10);

            await using var dbContext = CreateDbContext();
            var repo = new LegacyKeyWrappedEntityDtoRepo(dbContext, _mapper, _paginationProcessor);

            await repo.UpdateAsync(
                keyWrapper,
                new KeyWrappedEntityInputDto
                {
                    Id = seeded.Id,
                    ParentId = seeded.ParentId,
                    Name = "updated",
                    CreatedTimestamp = forgedCreatedTimestamp,
                    UpdatedTimestamp = seeded.UpdatedTimestamp
                }
            );

            var updated = await LoadKeyWrappedEntityAsync(seeded.ParentId, seeded.Id);
            updated.Name.ShouldBe("updated");
            updated.CreatedTimestamp.ShouldBe(seeded.CreatedTimestamp);
        }

        [Test]
        public async Task UpdateAsync_ShouldThrow_WhenEntityTimestampIsStale()
        {
            var seeded = await AddTrackedEntityAsync();
            var externallyUpdated = await UpdateTrackedEntityDirectlyAsync(seeded.Id, "server");

            await using var dbContext = CreateDbContext();
            var repo = new TrackedEntityRepo(dbContext, _mapper, _paginationProcessor);

            await Should.ThrowAsync<ConcurrentModificationException>(() =>
                repo.UpdateAsync(
                    new TrackedEntity
                    {
                        Id = seeded.Id,
                        Name = "client",
                        CreatedTimestamp = seeded.CreatedTimestamp,
                        UpdatedTimestamp = seeded.UpdatedTimestamp
                    }
                )
            );

            var current = await LoadTrackedEntityAsync(seeded.Id);
            current.Name.ShouldBe(externallyUpdated.Name);
        }

        [Test]
        public async Task UpdateAsync_ShouldEnforceConcurrency_ForChangeTrackingDtoInput()
        {
            var seeded = await AddTrackedEntityAsync();
            await UpdateTrackedEntityDirectlyAsync(seeded.Id, "server");

            await using var dbContext = CreateDbContext();
            var repo = new TrackedEntityDtoRepo(dbContext, _mapper, _paginationProcessor);

            await Should.ThrowAsync<ConcurrentModificationException>(() =>
                repo.UpdateAsync(
                    new TrackedEntityInputDto
                    {
                        Id = seeded.Id,
                        Name = "client",
                        CreatedTimestamp = seeded.CreatedTimestamp,
                        UpdatedTimestamp = seeded.UpdatedTimestamp
                    }
                )
            );
        }

        [Test]
        public async Task UpdateAsync_WithExplicitToken_ShouldSupportDtoWithoutTokenProperty()
        {
            var seeded = await AddTrackedEntityAsync();
            await Task.Delay(10);

            await using var dbContext = CreateDbContext();
            var repo = new LegacyTrackedEntityConcurrentDtoRepo(
                dbContext,
                _mapper,
                _paginationProcessor
            );

            await repo.UpdateAsync(
                new LegacyTrackedEntityInputDto
                {
                    Id = seeded.Id,
                    Name = "updated-with-explicit-token"
                },
                seeded.UpdatedTimestamp
            );

            var updated = await LoadTrackedEntityAsync(seeded.Id);
            updated.Name.ShouldBe("updated-with-explicit-token");
            updated.UpdatedTimestamp.ShouldNotBe(seeded.UpdatedTimestamp);
        }

        [Test]
        public async Task UpdateAsync_ShouldAdvanceUpdatedTimestamp_WhenKeyWrapperEntityTimestampMatches()
        {
            var seeded = await AddKeyWrappedEntityAsync();
            var keyWrapper = new ScopedKeyWrapper(seeded.ParentId, seeded.Id);
            await Task.Delay(10);

            await using var dbContext = CreateDbContext();
            var repo = new KeyWrappedEntityRepo(dbContext, _mapper, _paginationProcessor);

            await repo.UpdateAsync(
                keyWrapper,
                new KeyWrappedEntity
                {
                    Id = seeded.Id,
                    ParentId = seeded.ParentId,
                    Name = "updated",
                    CreatedTimestamp = seeded.CreatedTimestamp,
                    UpdatedTimestamp = seeded.UpdatedTimestamp
                }
            );

            var updated = await LoadKeyWrappedEntityAsync(seeded.ParentId, seeded.Id);
            updated.Name.ShouldBe("updated");
            updated.UpdatedTimestamp.ShouldNotBe(seeded.UpdatedTimestamp);
        }

        [Test]
        public async Task UpdateAsync_ShouldThrow_WhenKeyWrapperDtoTimestampIsStale()
        {
            var seeded = await AddKeyWrappedEntityAsync();
            await UpdateKeyWrappedEntityDirectlyAsync(seeded.ParentId, seeded.Id, "server");
            var keyWrapper = new ScopedKeyWrapper(seeded.ParentId, seeded.Id);

            await using var dbContext = CreateDbContext();
            var repo = new KeyWrappedEntityDtoRepo(dbContext, _mapper, _paginationProcessor);

            await Should.ThrowAsync<ConcurrentModificationException>(() =>
                repo.UpdateAsync(
                    keyWrapper,
                    new KeyWrappedEntityInputDto
                    {
                        Id = seeded.Id,
                        ParentId = seeded.ParentId,
                        Name = "client",
                        CreatedTimestamp = seeded.CreatedTimestamp,
                        UpdatedTimestamp = seeded.UpdatedTimestamp
                    }
                )
            );
        }

        [Test]
        public async Task RemoveAsync_WithExplicitToken_ShouldSoftDelete_WhenTimestampMatches()
        {
            var seeded = await AddTrackedEntityAsync();
            await Task.Delay(10);

            await using var dbContext = CreateDbContext();
            var repo = new TrackedEntityRepo(dbContext, _mapper, _paginationProcessor);

            await repo.RemoveAsync(seeded.Id, seeded.UpdatedTimestamp);

            var deleted = await LoadTrackedEntityAsync(seeded.Id);
            deleted.DeletedTimestamp.ShouldNotBeNull();
            deleted.UpdatedTimestamp.ShouldNotBe(seeded.UpdatedTimestamp);
        }

        [Test]
        public async Task RemoveAsync_WithExplicitToken_ShouldThrow_WhenSoftDeleteTimestampIsStale()
        {
            var seeded = await AddTrackedEntityAsync();
            await UpdateTrackedEntityDirectlyAsync(seeded.Id, "server");

            await using var dbContext = CreateDbContext();
            var repo = new TrackedEntityRepo(dbContext, _mapper, _paginationProcessor);

            await Should.ThrowAsync<ConcurrentModificationException>(() =>
                repo.RemoveAsync(seeded.Id, seeded.UpdatedTimestamp)
            );

            var current = await LoadTrackedEntityAsync(seeded.Id);
            current.DeletedTimestamp.ShouldBeNull();
        }

        [Test]
        public async Task RemoveAsync_WithoutToken_ShouldUseLegacyNonConcurrentDelete()
        {
            var seeded = await AddTrackedEntityAsync();
            await UpdateTrackedEntityDirectlyAsync(seeded.Id, "server");

            await using var dbContext = CreateDbContext();
            var repo = new LegacyTrackedEntityRepo(dbContext, _mapper, _paginationProcessor);

            await repo.RemoveAsync(seeded.Id);

            var deleted = await LoadTrackedEntityAsync(seeded.Id);
            deleted.DeletedTimestamp.ShouldNotBeNull();
        }

        [Test]
        public async Task ConcurrentRepo_RemoveAsync_WithoutToken_ShouldThrow()
        {
            var seeded = await AddTrackedEntityAsync();

            await using var dbContext = CreateDbContext();
            var repo = new TrackedEntityRepo(dbContext, _mapper, _paginationProcessor);

            var exception = await Should.ThrowAsync<InvalidOperationException>(() =>
                repo.RemoveAsync(seeded.Id)
            );

            exception.Message.ShouldContain("concurrency-aware repos");
        }

        [Test]
        public async Task GetConcurrencyTokenAsync_ShouldReturnCurrentTimestamp()
        {
            var seeded = await AddTrackedEntityAsync();

            await using var dbContext = CreateDbContext();
            var repo = new TrackedEntityRepo(dbContext, _mapper, _paginationProcessor);

            var token = await repo.GetConcurrencyTokenAsync(seeded.Id);

            token.ShouldBe(seeded.UpdatedTimestamp);
        }

        private CrudConcurrencyInMemoryDbContext CreateDbContext()
        {
            return new CrudConcurrencyInMemoryDbContext(_dbContextOptions);
        }

        private async Task<TrackedEntity> AddTrackedEntityAsync()
        {
            await using var dbContext = CreateDbContext();
            dbContext.TrackedEntities.Add(new TrackedEntity { Id = 1, Name = "initial" });
            await dbContext.SaveChangesAsync();

            return await dbContext.TrackedEntities.AsNoTracking().SingleAsync(x => x.Id == 1);
        }

        private async Task<TrackedEntity> UpdateTrackedEntityDirectlyAsync(int id, string name)
        {
            await Task.Delay(10);
            await using var dbContext = CreateDbContext();
            var entity = await dbContext.TrackedEntities.SingleAsync(x => x.Id == id);
            entity.Name = name;
            await dbContext.SaveChangesAsync();

            return await dbContext.TrackedEntities.AsNoTracking().SingleAsync(x => x.Id == id);
        }

        private async Task<TrackedEntity> LoadTrackedEntityAsync(int id)
        {
            await using var dbContext = CreateDbContext();
            return await dbContext.TrackedEntities.AsNoTracking().SingleAsync(x => x.Id == id);
        }

        private async Task<KeyWrappedEntity> AddKeyWrappedEntityAsync()
        {
            await using var dbContext = CreateDbContext();
            dbContext.KeyWrappedEntities.Add(
                new KeyWrappedEntity
                {
                    Id = 1,
                    ParentId = 99,
                    Name = "initial"
                }
            );
            await dbContext.SaveChangesAsync();

            return await dbContext.KeyWrappedEntities.AsNoTracking().SingleAsync(x => x.Id == 1);
        }

        private async Task<KeyWrappedEntity> UpdateKeyWrappedEntityDirectlyAsync(
            int parentId,
            int id,
            string name
        )
        {
            await Task.Delay(10);
            await using var dbContext = CreateDbContext();
            var entity = await dbContext.KeyWrappedEntities.SingleAsync(x =>
                x.ParentId == parentId && x.Id == id
            );
            entity.Name = name;
            await dbContext.SaveChangesAsync();

            return await dbContext.KeyWrappedEntities.AsNoTracking().SingleAsync(x =>
                x.ParentId == parentId && x.Id == id
            );
        }

        private async Task<KeyWrappedEntity> LoadKeyWrappedEntityAsync(int parentId, int id)
        {
            await using var dbContext = CreateDbContext();
            return await dbContext.KeyWrappedEntities.AsNoTracking().SingleAsync(x =>
                x.ParentId == parentId && x.Id == id
            );
        }

        private sealed class CrudConcurrencyInMemoryDbContext : DbContext
        {
            public CrudConcurrencyInMemoryDbContext(
                DbContextOptions<CrudConcurrencyInMemoryDbContext> options
            )
                : base(options) { }

            public DbSet<TrackedEntity> TrackedEntities => Set<TrackedEntity>();

            public DbSet<KeyWrappedEntity> KeyWrappedEntities => Set<KeyWrappedEntity>();
        }

        private sealed class TrackedEntity : IEntityWithId<int>, IAuditableEntity
        {
            public int Id { get; set; }

            public string Name { get; set; } = string.Empty;

            public DateTimeOffset CreatedTimestamp { get; set; }

            public DateTimeOffset? UpdatedTimestamp { get; set; }

            public DateTimeOffset? DeletedTimestamp { get; set; }
        }

        private sealed class KeyWrappedEntity : IEntity, IChangeTrackingEntity
        {
            public int Id { get; set; }

            public int ParentId { get; set; }

            public string Name { get; set; } = string.Empty;

            public DateTimeOffset CreatedTimestamp { get; set; }

            public DateTimeOffset? UpdatedTimestamp { get; set; }
        }

        private sealed class TrackedEntityDto : IDtoWithId<int>, IChangeTrackingDto
        {
            public int Id { get; set; }

            public string Name { get; set; } = string.Empty;

            public DateTimeOffset CreatedTimestamp { get; set; }

            public DateTimeOffset? UpdatedTimestamp { get; set; }
        }

        private sealed class TrackedEntityInputDto : IDtoWithId<int>, IChangeTrackingDto
        {
            public int Id { get; set; }

            public string Name { get; set; } = string.Empty;

            public DateTimeOffset CreatedTimestamp { get; set; }

            public DateTimeOffset? UpdatedTimestamp { get; set; }
        }

        private sealed class LegacyTrackedEntityInputDto : IDtoWithId<int>
        {
            public int Id { get; set; }

            public string Name { get; set; } = string.Empty;
        }

        private sealed class KeyWrappedEntityDto : IDto, IChangeTrackingDto
        {
            public int Id { get; set; }

            public int ParentId { get; set; }

            public string Name { get; set; } = string.Empty;

            public DateTimeOffset CreatedTimestamp { get; set; }

            public DateTimeOffset? UpdatedTimestamp { get; set; }
        }

        private sealed class KeyWrappedEntityInputDto : IDto, IChangeTrackingDto
        {
            public int Id { get; set; }

            public int ParentId { get; set; }

            public string Name { get; set; } = string.Empty;

            public DateTimeOffset CreatedTimestamp { get; set; }

            public DateTimeOffset? UpdatedTimestamp { get; set; }
        }

        private sealed class ScopedKeyWrapper : IKeyWrapper<KeyWrappedEntity>
        {
            public ScopedKeyWrapper(int parentId, int id)
            {
                ParentId = parentId;
                Id = id;
            }

            public int ParentId { get; }

            public int Id { get; private set; }

            public string[] GetKeyAsString()
            {
                return [ParentId.ToString(), Id.ToString()];
            }

            public void UpdateEntityWithContainingResource(KeyWrappedEntity entity)
            {
                entity.ParentId = ParentId;
            }

            public void UpdateKeyWrapperByEntity(KeyWrappedEntity entity)
            {
                Id = entity.Id;
            }

            public System.Linq.Expressions.Expression<Func<KeyWrappedEntity, bool>> GetKeyFilter()
            {
                return entity => entity.ParentId == ParentId && entity.Id == Id;
            }

            public System.Linq.Expressions.Expression<Func<KeyWrappedEntity, bool>> GetContainingResourceFilter()
            {
                return entity => entity.ParentId == ParentId;
            }
        }

        private sealed class LegacyTrackedEntityRepo : BaseCrudRepo<TrackedEntity, int>
        {
            public LegacyTrackedEntityRepo(
                DbContext dbContext,
                IMapper mapper,
                IPaginationProcessor paginationProcessor
            )
                : base(dbContext, mapper, paginationProcessor) { }
        }

        private sealed class TrackedEntityRepo
            : BaseConcurrentCrudRepo<TrackedEntity, int, DateTimeOffset?>
        {
            public TrackedEntityRepo(
                DbContext dbContext,
                IMapper mapper,
                IPaginationProcessor paginationProcessor
            )
                : base(
                    dbContext,
                    mapper,
                    paginationProcessor,
                    CrudConcurrencyConfiguration.UpdatedTimestamp(
                        nameof(TrackedEntity.UpdatedTimestamp),
                        nameof(TrackedEntity.UpdatedTimestamp)
                    )
                ) { }
        }

        private sealed class LegacyTrackedEntityDtoRepo
            : BaseCrudDtoRepo<TrackedEntity, int, TrackedEntityDto, TrackedEntityInputDto>
        {
            public LegacyTrackedEntityDtoRepo(
                DbContext dbContext,
                IMapper mapper,
                IPaginationProcessor paginationProcessor
            )
                : base(dbContext, mapper, paginationProcessor) { }
        }

        private sealed class TrackedEntityDtoRepo
            : BaseConcurrentCrudDtoRepo<
                TrackedEntity,
                int,
                TrackedEntityDto,
                TrackedEntityInputDto,
                DateTimeOffset?
            >
        {
            public TrackedEntityDtoRepo(
                DbContext dbContext,
                IMapper mapper,
                IPaginationProcessor paginationProcessor
            )
                : base(
                    dbContext,
                    mapper,
                    paginationProcessor,
                    CrudConcurrencyConfiguration.UpdatedTimestamp(
                        nameof(TrackedEntity.UpdatedTimestamp),
                        nameof(TrackedEntityInputDto.UpdatedTimestamp)
                    )
                ) { }
        }

        private sealed class LegacyTrackedEntityConcurrentDtoRepo
            : BaseConcurrentCrudDtoRepo<
                TrackedEntity,
                int,
                TrackedEntityDto,
                LegacyTrackedEntityInputDto,
                DateTimeOffset?
            >
        {
            public LegacyTrackedEntityConcurrentDtoRepo(
                DbContext dbContext,
                IMapper mapper,
                IPaginationProcessor paginationProcessor
            )
                : base(
                    dbContext,
                    mapper,
                    paginationProcessor,
                    CrudConcurrencyConfiguration.UpdatedTimestamp(
                        nameof(TrackedEntity.UpdatedTimestamp),
                        nameof(TrackedEntityInputDto.UpdatedTimestamp)
                    )
                ) { }
        }

        private sealed class LegacyKeyWrappedEntityRepo
            : BaseCrudRepoWithKeyWrapper<KeyWrappedEntity, ScopedKeyWrapper>
        {
            public LegacyKeyWrappedEntityRepo(
                DbContext dbContext,
                IMapper mapper,
                IPaginationProcessor paginationProcessor
            )
                : base(dbContext, mapper, paginationProcessor) { }
        }

        private sealed class KeyWrappedEntityRepo
            : BaseConcurrentCrudRepoWithKeyWrapper<
                KeyWrappedEntity,
                ScopedKeyWrapper,
                DateTimeOffset?
            >
        {
            public KeyWrappedEntityRepo(
                DbContext dbContext,
                IMapper mapper,
                IPaginationProcessor paginationProcessor
            )
                : base(
                    dbContext,
                    mapper,
                    paginationProcessor,
                    CrudConcurrencyConfiguration.UpdatedTimestamp(
                        nameof(KeyWrappedEntity.UpdatedTimestamp),
                        nameof(KeyWrappedEntity.UpdatedTimestamp)
                    )
                ) { }
        }

        private sealed class LegacyKeyWrappedEntityDtoRepo
            : BaseCrudDtoRepoWithKeyWrapper<
                KeyWrappedEntity,
                ScopedKeyWrapper,
                KeyWrappedEntityDto,
                KeyWrappedEntityInputDto
            >
        {
            public LegacyKeyWrappedEntityDtoRepo(
                DbContext dbContext,
                IMapper mapper,
                IPaginationProcessor paginationProcessor
            )
                : base(dbContext, mapper, paginationProcessor) { }
        }

        private sealed class KeyWrappedEntityDtoRepo
            : BaseConcurrentCrudDtoRepoWithKeyWrapper<
                KeyWrappedEntity,
                ScopedKeyWrapper,
                KeyWrappedEntityDto,
                KeyWrappedEntityInputDto,
                DateTimeOffset?
            >
        {
            public KeyWrappedEntityDtoRepo(
                DbContext dbContext,
                IMapper mapper,
                IPaginationProcessor paginationProcessor
            )
                : base(
                    dbContext,
                    mapper,
                    paginationProcessor,
                    CrudConcurrencyConfiguration.UpdatedTimestamp(
                        nameof(KeyWrappedEntity.UpdatedTimestamp),
                        nameof(KeyWrappedEntityInputDto.UpdatedTimestamp)
                    )
                ) { }
        }
    }
}
