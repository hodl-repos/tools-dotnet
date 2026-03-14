using AutoMapper;
using FluentValidation;
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
using tools_dotnet.Service.Abstract;

namespace tools_dotnet.Tests.CrudTest
{
    [TestFixture]
    public class CrudSoftDeleteLifecycleTests
    {
        private DbContextOptions<CrudSoftDeleteDbContext> _dbContextOptions = null!;
        private IMapper _mapper = null!;
        private PaginationProcessor _paginationProcessor = null!;

        [SetUp]
        public void Setup()
        {
            _dbContextOptions = new DbContextOptionsBuilder<CrudSoftDeleteDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
                .AddInterceptors(new TimestampsInterceptor())
                .Options;

            var mapperConfiguration = new MapperConfiguration(
                config =>
                {
                    config.CreateMap<SoftDeleteEntity, SoftDeleteEntityDto>().ReverseMap();
                    config.CreateMap<ScopedSoftDeleteEntity, ScopedSoftDeleteEntityDto>().ReverseMap();
                },
                NullLoggerFactory.Instance
            );

            _mapper = mapperConfiguration.CreateMapper();
            _paginationProcessor = new PaginationProcessor();
        }

        [Test]
        public async Task GetByIdAsync_ShouldHideSoftDeletedEntity_ButIncludingDeletedShouldReturnIt()
        {
            var seeded = await AddSoftDeleteEntityAsync(1, "hidden");

            await using (var deleteContext = CreateDbContext())
            {
                var repo = new LegacySoftDeleteRepo(deleteContext, _mapper, _paginationProcessor);
                await repo.RemoveAsync(seeded.Id);
            }

            await using var dbContext = CreateDbContext();
            var repoAfterDelete = new LegacySoftDeleteRepo(dbContext, _mapper, _paginationProcessor);

            await Should.ThrowAsync<ItemNotFoundException>(() => repoAfterDelete.GetByIdAsync(seeded.Id));

            var deleted = await repoAfterDelete.GetByIdIncludingDeletedAsync(seeded.Id);
            deleted.Id.ShouldBe(seeded.Id);
            deleted.DeletedTimestamp.ShouldNotBeNull();
        }

        [Test]
        public async Task GetAllDeletedAsync_ShouldReturnOnlySoftDeletedEntities()
        {
            var deleted = await AddSoftDeleteEntityAsync(1, "deleted");
            await AddSoftDeleteEntityAsync(2, "active");

            await using (var deleteContext = CreateDbContext())
            {
                var repo = new LegacySoftDeleteRepo(deleteContext, _mapper, _paginationProcessor);
                await repo.RemoveAsync(deleted.Id);
            }

            await using var dbContext = CreateDbContext();
            var repoAfterDelete = new LegacySoftDeleteRepo(dbContext, _mapper, _paginationProcessor);

            var deletedItems = (await repoAfterDelete.GetAllDeletedAsync()).ToList();
            deletedItems.Count.ShouldBe(1);
            deletedItems[0].Id.ShouldBe(deleted.Id);

            var includingDeletedItems = (await repoAfterDelete.GetAllIncludingDeletedAsync()).ToList();
            includingDeletedItems.Count.ShouldBe(2);
        }

        [Test]
        public async Task RestoreAsync_ShouldMakeDeletedEntityVisibleAgain()
        {
            var seeded = await AddSoftDeleteEntityAsync(1, "restore-me");

            await using (var deleteContext = CreateDbContext())
            {
                var repo = new LegacySoftDeleteRepo(deleteContext, _mapper, _paginationProcessor);
                await repo.RemoveAsync(seeded.Id);
            }

            await using (var restoreContext = CreateDbContext())
            {
                var repo = new LegacySoftDeleteRepo(restoreContext, _mapper, _paginationProcessor);
                await repo.RestoreAsync(seeded.Id);
            }

            await using var dbContext = CreateDbContext();
            var repoAfterRestore = new LegacySoftDeleteRepo(dbContext, _mapper, _paginationProcessor);

            var restored = await repoAfterRestore.GetByIdAsync(seeded.Id);
            restored.DeletedTimestamp.ShouldBeNull();
        }

        [Test]
        public async Task HardRemoveAsync_ShouldPhysicallyDeleteEntity()
        {
            var seeded = await AddSoftDeleteEntityAsync(1, "hard-delete-me");

            await using (var dbContext = CreateDbContext())
            {
                var repo = new LegacySoftDeleteRepo(dbContext, _mapper, _paginationProcessor);
                await repo.HardRemoveAsync(seeded.Id);
            }

            await using var verifyContext = CreateDbContext();
            var exists = await verifyContext.SoftDeleteEntities.AnyAsync(x => x.Id == seeded.Id);
            exists.ShouldBeFalse();
        }

        [Test]
        public async Task ConcurrentRestoreAsync_ShouldRequireExplicitToken()
        {
            var seeded = await AddSoftDeleteEntityAsync(1, "concurrent");

            await using var dbContext = CreateDbContext();
            var repo = new ConcurrentSoftDeleteRepo(dbContext, _mapper, _paginationProcessor);

            var exception = await Should.ThrowAsync<InvalidOperationException>(() =>
                repo.RestoreAsync(seeded.Id)
            );

            exception.Message.ShouldContain("concurrency-aware repos");
        }

        [Test]
        public async Task ConcurrentRestoreAsync_WithToken_ShouldRestoreDeletedEntity()
        {
            var seeded = await AddSoftDeleteEntityAsync(1, "restore-concurrent");

            await using (var deleteContext = CreateDbContext())
            {
                var repo = new ConcurrentSoftDeleteRepo(
                    deleteContext,
                    _mapper,
                    _paginationProcessor
                );
                var token = await repo.GetConcurrencyTokenAsync(seeded.Id);
                await repo.RemoveAsync(seeded.Id, token);
            }

            await using (var restoreContext = CreateDbContext())
            {
                var repo = new ConcurrentSoftDeleteRepo(
                    restoreContext,
                    _mapper,
                    _paginationProcessor
                );
                var deletedToken = await repo.GetConcurrencyTokenAsync(seeded.Id);
                await repo.RestoreAsync(seeded.Id, deletedToken);
            }

            await using var verifyContext = CreateDbContext();
            var repoAfterRestore = new ConcurrentSoftDeleteRepo(
                verifyContext,
                _mapper,
                _paginationProcessor
            );
            var restored = await repoAfterRestore.GetByIdAsync(seeded.Id);
            restored.DeletedTimestamp.ShouldBeNull();
        }

        [Test]
        public async Task ConcurrentHardRemoveAsync_WithStaleToken_ShouldThrow()
        {
            var seeded = await AddSoftDeleteEntityAsync(1, "stale");

            await using var staleContext = CreateDbContext();
            var repo = new ConcurrentSoftDeleteRepo(staleContext, _mapper, _paginationProcessor);
            var staleToken = await repo.GetConcurrencyTokenAsync(seeded.Id);

            await UpdateSoftDeleteEntityDirectlyAsync(seeded.Id, "server-change");

            await Should.ThrowAsync<ConcurrentModificationException>(() =>
                repo.HardRemoveAsync(seeded.Id, staleToken)
            );
        }

        [Test]
        public async Task ServiceApis_ShouldExposeDeletedReads()
        {
            var deleted = await AddSoftDeleteEntityAsync(1, "deleted");
            await AddSoftDeleteEntityAsync(2, "active");

            await using (var deleteContext = CreateDbContext())
            {
                var repo = new LegacySoftDeleteRepo(deleteContext, _mapper, _paginationProcessor);
                await repo.RemoveAsync(deleted.Id);
            }

            await using var dbContext = CreateDbContext();
            var service = new LegacySoftDeleteService(
                _mapper,
                new LegacySoftDeleteRepo(dbContext, _mapper, _paginationProcessor),
                new SoftDeleteEntityValidator()
            );

            var deletedDtos = (await service.GetAllDeletedAsync()).ToList();
            deletedDtos.Count.ShouldBe(1);
            deletedDtos[0].Id.ShouldBe(deleted.Id);

            await Should.ThrowAsync<ItemNotFoundException>(() => service.GetByIdAsync(deleted.Id));

            var deletedDto = await service.GetByIdIncludingDeletedAsync(deleted.Id);
            deletedDto.DeletedTimestamp.ShouldNotBeNull();
        }

        [Test]
        public async Task ConcurrentService_HardRemoveAsync_WithoutToken_ShouldThrow()
        {
            var seeded = await AddSoftDeleteEntityAsync(1, "service");

            await using var dbContext = CreateDbContext();
            var service = new ConcurrentSoftDeleteService(
                _mapper,
                new ConcurrentSoftDeleteRepo(dbContext, _mapper, _paginationProcessor),
                new SoftDeleteEntityValidator()
            );

            var exception = await Should.ThrowAsync<InvalidOperationException>(() =>
                service.HardRemoveAsync(seeded.Id)
            );

            exception.Message.ShouldContain("concurrency-aware services");
        }

        [Test]
        public async Task KeyWrapperService_RestoreAsync_ShouldRestoreDeletedScopedEntity()
        {
            var seeded = await AddScopedSoftDeleteEntityAsync(42, 1, "scoped");
            var keyWrapper = new ScopedSoftDeleteKeyWrapper(seeded.ParentId, seeded.Id);

            await using (var deleteContext = CreateDbContext())
            {
                var repo = new LegacyScopedSoftDeleteRepo(
                    deleteContext,
                    _mapper,
                    _paginationProcessor
                );
                await repo.RemoveAsync(keyWrapper);
            }

            await using (var restoreContext = CreateDbContext())
            {
                var service = new LegacyScopedSoftDeleteService(
                    _mapper,
                    new LegacyScopedSoftDeleteRepo(
                        restoreContext,
                        _mapper,
                        _paginationProcessor
                    ),
                    new ScopedSoftDeleteEntityValidator()
                );

                await Should.ThrowAsync<ItemNotFoundException>(() => service.GetByIdAsync(keyWrapper));
                var deleted = await service.GetByIdIncludingDeletedAsync(keyWrapper);
                deleted.DeletedTimestamp.ShouldNotBeNull();

                await service.RestoreAsync(keyWrapper);
            }

            await using var verifyContext = CreateDbContext();
            var verifyService = new LegacyScopedSoftDeleteService(
                _mapper,
                new LegacyScopedSoftDeleteRepo(verifyContext, _mapper, _paginationProcessor),
                new ScopedSoftDeleteEntityValidator()
            );
            var restored = await verifyService.GetByIdAsync(keyWrapper);
            restored.DeletedTimestamp.ShouldBeNull();
        }

        private CrudSoftDeleteDbContext CreateDbContext()
        {
            return new CrudSoftDeleteDbContext(_dbContextOptions);
        }

        private async Task<SoftDeleteEntity> AddSoftDeleteEntityAsync(int id, string name)
        {
            await using var dbContext = CreateDbContext();
            dbContext.SoftDeleteEntities.Add(new SoftDeleteEntity { Id = id, Name = name });
            await dbContext.SaveChangesAsync();

            return await dbContext.SoftDeleteEntities.AsNoTracking().SingleAsync(x => x.Id == id);
        }

        private async Task UpdateSoftDeleteEntityDirectlyAsync(int id, string name)
        {
            await Task.Delay(10);
            await using var dbContext = CreateDbContext();
            var entity = await dbContext.SoftDeleteEntities.SingleAsync(x => x.Id == id);
            entity.Name = name;
            await dbContext.SaveChangesAsync();
        }

        private async Task<ScopedSoftDeleteEntity> AddScopedSoftDeleteEntityAsync(
            int parentId,
            int id,
            string name
        )
        {
            await using var dbContext = CreateDbContext();
            dbContext.ScopedSoftDeleteEntities.Add(
                new ScopedSoftDeleteEntity
                {
                    Id = id,
                    ParentId = parentId,
                    Name = name
                }
            );
            await dbContext.SaveChangesAsync();

            return await dbContext.ScopedSoftDeleteEntities.AsNoTracking().SingleAsync(x =>
                x.ParentId == parentId && x.Id == id
            );
        }

        private sealed class CrudSoftDeleteDbContext : DbContext
        {
            public CrudSoftDeleteDbContext(DbContextOptions<CrudSoftDeleteDbContext> options)
                : base(options) { }

            public DbSet<SoftDeleteEntity> SoftDeleteEntities => Set<SoftDeleteEntity>();

            public DbSet<ScopedSoftDeleteEntity> ScopedSoftDeleteEntities =>
                Set<ScopedSoftDeleteEntity>();
        }

        private sealed class SoftDeleteEntity : IEntityWithId<int>, IAuditableEntity
        {
            public int Id { get; set; }

            public string Name { get; set; } = string.Empty;

            public DateTimeOffset CreatedTimestamp { get; set; }

            public DateTimeOffset? UpdatedTimestamp { get; set; }

            public DateTimeOffset? DeletedTimestamp { get; set; }
        }

        private sealed class ScopedSoftDeleteEntity : IEntity, IAuditableEntity
        {
            public int Id { get; set; }

            public int ParentId { get; set; }

            public string Name { get; set; } = string.Empty;

            public DateTimeOffset CreatedTimestamp { get; set; }

            public DateTimeOffset? UpdatedTimestamp { get; set; }

            public DateTimeOffset? DeletedTimestamp { get; set; }
        }

        private sealed class SoftDeleteEntityDto : IDtoWithId<int>, IChangeTrackingDto
        {
            public int Id { get; set; }

            public string Name { get; set; } = string.Empty;

            public DateTimeOffset CreatedTimestamp { get; set; }

            public DateTimeOffset? UpdatedTimestamp { get; set; }

            public DateTimeOffset? DeletedTimestamp { get; set; }
        }

        private sealed class ScopedSoftDeleteEntityDto : IDto, IChangeTrackingDto
        {
            public int Id { get; set; }

            public int ParentId { get; set; }

            public string Name { get; set; } = string.Empty;

            public DateTimeOffset CreatedTimestamp { get; set; }

            public DateTimeOffset? UpdatedTimestamp { get; set; }

            public DateTimeOffset? DeletedTimestamp { get; set; }
        }

        private sealed class ScopedSoftDeleteKeyWrapper : IKeyWrapper<ScopedSoftDeleteEntity>
        {
            public ScopedSoftDeleteKeyWrapper(int parentId, int id)
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

            public void UpdateEntityWithContainingResource(ScopedSoftDeleteEntity entity)
            {
                entity.ParentId = ParentId;
            }

            public void UpdateKeyWrapperByEntity(ScopedSoftDeleteEntity entity)
            {
                Id = entity.Id;
            }

            public System.Linq.Expressions.Expression<Func<ScopedSoftDeleteEntity, bool>> GetKeyFilter()
            {
                return entity => entity.ParentId == ParentId && entity.Id == Id;
            }

            public System.Linq.Expressions.Expression<Func<ScopedSoftDeleteEntity, bool>> GetContainingResourceFilter()
            {
                return entity => entity.ParentId == ParentId;
            }
        }

        private sealed class LegacySoftDeleteRepo
            : BaseCrudDtoRepo<SoftDeleteEntity, int, SoftDeleteEntityDto>
        {
            public LegacySoftDeleteRepo(
                DbContext dbContext,
                IMapper mapper,
                IPaginationProcessor paginationProcessor
            )
                : base(dbContext, mapper, paginationProcessor) { }
        }

        private sealed class ConcurrentSoftDeleteRepo
            : BaseConcurrentCrudDtoRepo<SoftDeleteEntity, int, SoftDeleteEntityDto, DateTimeOffset?>
        {
            public ConcurrentSoftDeleteRepo(
                DbContext dbContext,
                IMapper mapper,
                IPaginationProcessor paginationProcessor
            )
                : base(
                    dbContext,
                    mapper,
                    paginationProcessor,
                    CrudConcurrencyConfiguration.UpdatedTimestamp(
                        nameof(SoftDeleteEntity.UpdatedTimestamp),
                        nameof(SoftDeleteEntityDto.UpdatedTimestamp)
                    )
                ) { }
        }

        private sealed class LegacyScopedSoftDeleteRepo
            : BaseCrudDtoRepoWithKeyWrapper<
                ScopedSoftDeleteEntity,
                ScopedSoftDeleteKeyWrapper,
                ScopedSoftDeleteEntityDto
            >
        {
            public LegacyScopedSoftDeleteRepo(
                DbContext dbContext,
                IMapper mapper,
                IPaginationProcessor paginationProcessor
            )
                : base(dbContext, mapper, paginationProcessor) { }
        }

        private sealed class LegacySoftDeleteService
            : BaseCrudService<
                SoftDeleteEntity,
                int,
                SoftDeleteEntityDto,
                LegacySoftDeleteRepo,
                SoftDeleteEntityValidator
            >
        {
            public LegacySoftDeleteService(
                IMapper mapper,
                LegacySoftDeleteRepo baseRepo,
                SoftDeleteEntityValidator validator
            )
                : base(mapper, baseRepo, validator) { }
        }

        private sealed class ConcurrentSoftDeleteService
            : BaseConcurrentCrudService<
                SoftDeleteEntity,
                int,
                SoftDeleteEntityDto,
                DateTimeOffset?,
                ConcurrentSoftDeleteRepo,
                SoftDeleteEntityValidator
            >
        {
            public ConcurrentSoftDeleteService(
                IMapper mapper,
                ConcurrentSoftDeleteRepo baseRepo,
                SoftDeleteEntityValidator validator
            )
                : base(mapper, baseRepo, validator) { }
        }

        private sealed class LegacyScopedSoftDeleteService
            : BaseCrudServiceWithKeyWrapper<
                ScopedSoftDeleteEntity,
                ScopedSoftDeleteKeyWrapper,
                ScopedSoftDeleteEntityDto,
                LegacyScopedSoftDeleteRepo,
                ScopedSoftDeleteEntityValidator
            >
        {
            public LegacyScopedSoftDeleteService(
                IMapper mapper,
                LegacyScopedSoftDeleteRepo baseRepo,
                ScopedSoftDeleteEntityValidator validator
            )
                : base(mapper, baseRepo, validator) { }

            protected override Task SetAndValidateKeyAsync(
                ScopedSoftDeleteEntityDto item,
                ScopedSoftDeleteKeyWrapper keyWrapper
            )
            {
                item.ParentId = keyWrapper.ParentId;
                item.Id = keyWrapper.Id;
                return Task.CompletedTask;
            }
        }

        private sealed class SoftDeleteEntityValidator : AbstractValidator<SoftDeleteEntityDto>
        {
            public SoftDeleteEntityValidator() { }
        }

        private sealed class ScopedSoftDeleteEntityValidator
            : AbstractValidator<ScopedSoftDeleteEntityDto>
        {
            public ScopedSoftDeleteEntityValidator() { }
        }
    }
}
