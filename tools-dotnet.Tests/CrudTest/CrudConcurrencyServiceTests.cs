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
using tools_dotnet.Pagination.Services;
using tools_dotnet.Paging.Impl;
using tools_dotnet.Service.Abstract;

namespace tools_dotnet.Tests.CrudTest
{
    [TestFixture]
    public class CrudConcurrencyServiceTests
    {
        private DbContextOptions<CrudConcurrencyServiceDbContext> _dbContextOptions = null!;
        private IMapper _mapper = null!;
        private PaginationProcessor _paginationProcessor = null!;

        [SetUp]
        public void Setup()
        {
            _dbContextOptions = new DbContextOptionsBuilder<CrudConcurrencyServiceDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
                .AddInterceptors(new TimestampsInterceptor())
                .Options;

            var mapperConfiguration = new MapperConfiguration(
                config =>
                {
                    config.CreateMap<ServiceTrackedEntity, ServiceTrackedEntityDto>().ReverseMap();
                    config.CreateMap<ServiceScopedEntity, ServiceScopedEntityDto>().ReverseMap();
                },
                NullLoggerFactory.Instance
            );

            _mapper = mapperConfiguration.CreateMapper();
            _paginationProcessor = new PaginationProcessor();
        }

        [Test]
        public async Task ConcurrentService_GetConcurrencyTokenAsync_ShouldReturnCurrentTimestamp()
        {
            var seeded = await AddTrackedEntityAsync();

            await using var dbContext = CreateDbContext();
            var service = new ServiceTrackedEntityConcurrentService(
                _mapper,
                new ServiceTrackedEntityConcurrentRepo(dbContext, _mapper, _paginationProcessor),
                new ServiceTrackedEntityValidator()
            );

            var token = await service.GetConcurrencyTokenAsync(seeded.Id);

            token.ShouldBe(seeded.UpdatedTimestamp);
        }

        [Test]
        public async Task ConcurrentService_RemoveAsync_WithExplicitToken_ShouldSoftDeleteEntity()
        {
            var seeded = await AddTrackedEntityAsync();

            await using var dbContext = CreateDbContext();
            var service = new ServiceTrackedEntityConcurrentService(
                _mapper,
                new ServiceTrackedEntityConcurrentRepo(dbContext, _mapper, _paginationProcessor),
                new ServiceTrackedEntityValidator()
            );
            var token = await service.GetConcurrencyTokenAsync(seeded.Id);

            await service.RemoveAsync(seeded.Id, token);

            var deleted = await LoadTrackedEntityAsync(seeded.Id);
            deleted.DeletedTimestamp.ShouldNotBeNull();
        }

        [Test]
        public async Task LegacyService_RemoveAsync_ShouldKeepOldNonConcurrentBehavior()
        {
            var seeded = await AddTrackedEntityAsync();
            await UpdateTrackedEntityDirectlyAsync(seeded.Id, "server");

            await using var dbContext = CreateDbContext();
            var service = new ServiceTrackedEntityLegacyService(
                _mapper,
                new ServiceTrackedEntityLegacyRepo(dbContext, _mapper, _paginationProcessor),
                new ServiceTrackedEntityValidator()
            );

            await service.RemoveAsync(seeded.Id);

            var deleted = await LoadTrackedEntityAsync(seeded.Id);
            deleted.DeletedTimestamp.ShouldNotBeNull();
        }

        [Test]
        public async Task ConcurrentKeyWrapperService_UpdateAsync_WithExplicitToken_ShouldUpdateEntity()
        {
            var seeded = await AddScopedEntityAsync();
            var keyWrapper = new ServiceScopedEntityKey(seeded.ParentId, seeded.Id);
            var token = seeded.UpdatedTimestamp;
            await Task.Delay(10);

            await using var dbContext = CreateDbContext();
            var service = new ServiceScopedEntityConcurrentService(
                _mapper,
                new ServiceScopedEntityConcurrentRepo(dbContext, _mapper, _paginationProcessor),
                new ServiceScopedEntityValidator()
            );

            await service.UpdateAsync(
                keyWrapper,
                new ServiceScopedEntityDto
                {
                    Id = seeded.Id,
                    ParentId = seeded.ParentId,
                    Name = "updated",
                    CreatedTimestamp = seeded.CreatedTimestamp,
                    UpdatedTimestamp = seeded.UpdatedTimestamp
                },
                token
            );

            var updated = await LoadScopedEntityAsync(seeded.ParentId, seeded.Id);
            updated.Name.ShouldBe("updated");
            updated.UpdatedTimestamp.ShouldNotBe(seeded.UpdatedTimestamp);
        }

        [Test]
        public async Task LegacyService_GetAllAsync_ShouldObserveCancellationToken()
        {
            await AddTrackedEntityAsync();

            await using var dbContext = CreateDbContext();
            var service = new ServiceTrackedEntityLegacyService(
                _mapper,
                new ServiceTrackedEntityLegacyRepo(dbContext, _mapper, _paginationProcessor),
                new ServiceTrackedEntityValidator()
            );
            using var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            await Should.ThrowAsync<OperationCanceledException>(() =>
                service.GetAllAsync(cancellationTokenSource.Token)
            );
        }

        [Test]
        public async Task LegacyService_GetAllAsyncPaged_ShouldObserveCancellationToken()
        {
            await AddTrackedEntityAsync();

            await using var dbContext = CreateDbContext();
            var service = new ServiceTrackedEntityLegacyService(
                _mapper,
                new ServiceTrackedEntityLegacyRepo(dbContext, _mapper, _paginationProcessor),
                new ServiceTrackedEntityValidator()
            );
            using var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            await Should.ThrowAsync<OperationCanceledException>(() =>
                service.GetAllAsync(new ApiPagination(), cancellationTokenSource.Token)
            );
        }

        private CrudConcurrencyServiceDbContext CreateDbContext()
        {
            return new CrudConcurrencyServiceDbContext(_dbContextOptions);
        }

        private async Task<ServiceTrackedEntity> AddTrackedEntityAsync()
        {
            await using var dbContext = CreateDbContext();
            dbContext.ServiceTrackedEntities.Add(
                new ServiceTrackedEntity { Id = 1, Name = "initial" }
            );
            await dbContext.SaveChangesAsync();

            return await dbContext.ServiceTrackedEntities.AsNoTracking().SingleAsync(x => x.Id == 1);
        }

        private async Task UpdateTrackedEntityDirectlyAsync(int id, string name)
        {
            await Task.Delay(10);
            await using var dbContext = CreateDbContext();
            var entity = await dbContext.ServiceTrackedEntities.SingleAsync(x => x.Id == id);
            entity.Name = name;
            await dbContext.SaveChangesAsync();
        }

        private async Task<ServiceTrackedEntity> LoadTrackedEntityAsync(int id)
        {
            await using var dbContext = CreateDbContext();
            return await dbContext.ServiceTrackedEntities.AsNoTracking().SingleAsync(x => x.Id == id);
        }

        private async Task<ServiceScopedEntity> AddScopedEntityAsync()
        {
            await using var dbContext = CreateDbContext();
            dbContext.ServiceScopedEntities.Add(
                new ServiceScopedEntity { Id = 1, ParentId = 10, Name = "initial" }
            );
            await dbContext.SaveChangesAsync();

            return await dbContext.ServiceScopedEntities.AsNoTracking().SingleAsync(x => x.Id == 1);
        }

        private async Task<ServiceScopedEntity> LoadScopedEntityAsync(int parentId, int id)
        {
            await using var dbContext = CreateDbContext();
            return await dbContext.ServiceScopedEntities.AsNoTracking().SingleAsync(x =>
                x.ParentId == parentId && x.Id == id
            );
        }

        private sealed class CrudConcurrencyServiceDbContext : DbContext
        {
            public CrudConcurrencyServiceDbContext(
                DbContextOptions<CrudConcurrencyServiceDbContext> options
            )
                : base(options) { }

            public DbSet<ServiceTrackedEntity> ServiceTrackedEntities => Set<ServiceTrackedEntity>();

            public DbSet<ServiceScopedEntity> ServiceScopedEntities => Set<ServiceScopedEntity>();
        }

        private sealed class ServiceTrackedEntity : IEntityWithId<int>, IAuditableEntity
        {
            public int Id { get; set; }

            public string Name { get; set; } = string.Empty;

            public DateTimeOffset CreatedTimestamp { get; set; }

            public DateTimeOffset? UpdatedTimestamp { get; set; }

            public DateTimeOffset? DeletedTimestamp { get; set; }
        }

        private sealed class ServiceScopedEntity : IEntity, IChangeTrackingEntity
        {
            public int Id { get; set; }

            public int ParentId { get; set; }

            public string Name { get; set; } = string.Empty;

            public DateTimeOffset CreatedTimestamp { get; set; }

            public DateTimeOffset? UpdatedTimestamp { get; set; }
        }

        private sealed class ServiceTrackedEntityDto : IDtoWithId<int>, IChangeTrackingDto
        {
            public int Id { get; set; }

            public string Name { get; set; } = string.Empty;

            public DateTimeOffset CreatedTimestamp { get; set; }

            public DateTimeOffset? UpdatedTimestamp { get; set; }
        }

        private sealed class ServiceScopedEntityDto : IDto, IChangeTrackingDto
        {
            public int Id { get; set; }

            public int ParentId { get; set; }

            public string Name { get; set; } = string.Empty;

            public DateTimeOffset CreatedTimestamp { get; set; }

            public DateTimeOffset? UpdatedTimestamp { get; set; }
        }

        private sealed class ServiceScopedEntityKey : IKeyWrapper<ServiceScopedEntity>
        {
            public ServiceScopedEntityKey(int parentId, int id)
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

            public void UpdateEntityWithContainingResource(ServiceScopedEntity entity)
            {
                entity.ParentId = ParentId;
            }

            public void UpdateKeyWrapperByEntity(ServiceScopedEntity entity)
            {
                Id = entity.Id;
            }

            public System.Linq.Expressions.Expression<Func<ServiceScopedEntity, bool>> GetKeyFilter()
            {
                return entity => entity.ParentId == ParentId && entity.Id == Id;
            }

            public System.Linq.Expressions.Expression<Func<ServiceScopedEntity, bool>> GetContainingResourceFilter()
            {
                return entity => entity.ParentId == ParentId;
            }
        }

        private sealed class ServiceTrackedEntityLegacyRepo
            : BaseCrudDtoRepo<ServiceTrackedEntity, int, ServiceTrackedEntityDto>
        {
            public ServiceTrackedEntityLegacyRepo(
                DbContext dbContext,
                IMapper mapper,
                IPaginationProcessor paginationProcessor
            )
                : base(dbContext, mapper, paginationProcessor) { }
        }

        private sealed class ServiceTrackedEntityConcurrentRepo
            : BaseConcurrentCrudDtoRepo<
                ServiceTrackedEntity,
                int,
                ServiceTrackedEntityDto,
                DateTimeOffset?
            >
        {
            public ServiceTrackedEntityConcurrentRepo(
                DbContext dbContext,
                IMapper mapper,
                IPaginationProcessor paginationProcessor
            )
                : base(
                    dbContext,
                    mapper,
                    paginationProcessor,
                    CrudConcurrencyConfiguration.UpdatedTimestamp(
                        nameof(ServiceTrackedEntity.UpdatedTimestamp),
                        nameof(ServiceTrackedEntityDto.UpdatedTimestamp)
                    )
                ) { }
        }

        private sealed class ServiceScopedEntityConcurrentRepo
            : BaseConcurrentCrudDtoRepoWithKeyWrapper<
                ServiceScopedEntity,
                ServiceScopedEntityKey,
                ServiceScopedEntityDto,
                DateTimeOffset?
            >
        {
            public ServiceScopedEntityConcurrentRepo(
                DbContext dbContext,
                IMapper mapper,
                IPaginationProcessor paginationProcessor
            )
                : base(
                    dbContext,
                    mapper,
                    paginationProcessor,
                    CrudConcurrencyConfiguration.UpdatedTimestamp(
                        nameof(ServiceScopedEntity.UpdatedTimestamp),
                        nameof(ServiceScopedEntityDto.UpdatedTimestamp)
                    )
                ) { }
        }

        private sealed class ServiceTrackedEntityLegacyService
            : BaseCrudService<
                ServiceTrackedEntity,
                int,
                ServiceTrackedEntityDto,
                ServiceTrackedEntityLegacyRepo,
                ServiceTrackedEntityValidator
            >
        {
            public ServiceTrackedEntityLegacyService(
                IMapper mapper,
                ServiceTrackedEntityLegacyRepo baseRepo,
                ServiceTrackedEntityValidator validator
            )
                : base(mapper, baseRepo, validator) { }
        }

        private sealed class ServiceTrackedEntityConcurrentService
            : BaseConcurrentCrudService<
                ServiceTrackedEntity,
                int,
                ServiceTrackedEntityDto,
                DateTimeOffset?,
                ServiceTrackedEntityConcurrentRepo,
                ServiceTrackedEntityValidator
            >
        {
            public ServiceTrackedEntityConcurrentService(
                IMapper mapper,
                ServiceTrackedEntityConcurrentRepo baseRepo,
                ServiceTrackedEntityValidator validator
            )
                : base(mapper, baseRepo, validator) { }
        }

        private sealed class ServiceScopedEntityConcurrentService
            : BaseConcurrentCrudServiceWithKeyWrapper<
                ServiceScopedEntity,
                ServiceScopedEntityKey,
                ServiceScopedEntityDto,
                DateTimeOffset?,
                ServiceScopedEntityConcurrentRepo,
                ServiceScopedEntityValidator
            >
        {
            public ServiceScopedEntityConcurrentService(
                IMapper mapper,
                ServiceScopedEntityConcurrentRepo baseRepo,
                ServiceScopedEntityValidator validator
            )
                : base(mapper, baseRepo, validator) { }

            protected override Task SetAndValidateKeyAsync(
                ServiceScopedEntityDto item,
                ServiceScopedEntityKey keyWrapper
            )
            {
                item.ParentId = keyWrapper.ParentId;
                item.Id = keyWrapper.Id;
                return Task.CompletedTask;
            }
        }

        private sealed class ServiceTrackedEntityValidator : AbstractValidator<ServiceTrackedEntityDto>
        {
            public ServiceTrackedEntityValidator() { }
        }

        private sealed class ServiceScopedEntityValidator : AbstractValidator<ServiceScopedEntityDto>
        {
            public ServiceScopedEntityValidator() { }
        }
    }
}
