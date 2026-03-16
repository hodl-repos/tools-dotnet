using System.Linq;
using AutoMapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Testcontainers.MsSql;
using tools_dotnet.Dao.Crud;
using tools_dotnet.Dao.Crud.Impl;
using tools_dotnet.Dao.Entity;
using tools_dotnet.Dto;
using tools_dotnet.Exceptions;
using tools_dotnet.Pagination.Services;

namespace tools_dotnet.Tests.CrudTest
{
    [TestFixture]
    public class CrudMsSqlConcurrencyIntegrationTests
    {
        private readonly MsSqlContainer _msSqlContainer = new MsSqlBuilder(
            "mcr.microsoft.com/mssql/server:2022-latest"
        )
            .WithPassword("Your_strong_password123!")
            .WithCleanUp(true)
            .Build();

        private DbContextOptions<CrudMsSqlTestDbContext> _dbContextOptions = null!;
        private IMapper _mapper = null!;
        private PaginationProcessor _paginationProcessor = null!;

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

            var mapperConfiguration = new MapperConfiguration(
                config =>
                {
                    config.CreateMap<RowVersionEntity, RowVersionEntityDto>().ReverseMap();
                    config.CreateMap<RowVersionEntityInputDto, RowVersionEntity>();
                    config.CreateMap<SoftDeleteRowVersionEntity, SoftDeleteRowVersionEntityDto>()
                        .ReverseMap();
                    config.CreateMap<GuidTokenEntity, GuidTokenEntityDto>().ReverseMap();
                    config.CreateMap<GuidTokenUpdateDto, GuidTokenEntity>();
                },
                NullLoggerFactory.Instance
            );

            _mapper = mapperConfiguration.CreateMapper();
            _paginationProcessor = new PaginationProcessor();

            await using var dbContext = new CrudMsSqlTestDbContext(_dbContextOptions);
            await dbContext.Database.EnsureCreatedAsync();
            await EnsureGuidTokenTriggerAsync(dbContext);
        }

        [Test]
        public async Task UpdateAsync_WithExplicitToken_ShouldThrow_WhenRowVersionIsStale()
        {
            var seeded = await AddRowVersionEntityAsync();
            await UpdateRowVersionEntityDirectlyAsync(seeded.Id, "server");

            await using var dbContext = new CrudMsSqlTestDbContext(_dbContextOptions);
            var repo = new RowVersionEntityRepo(dbContext, _mapper, _paginationProcessor);

            await Should.ThrowAsync<ConcurrentModificationException>(() =>
                repo.UpdateAsync(
                    new RowVersionEntityInputDto
                    {
                        Id = seeded.Id,
                        Name = "client",
                        RowVersion = seeded.RowVersion
                    },
                    seeded.RowVersion
                )
            );
        }

        [Test]
        public async Task RemoveAsync_ShouldThrow_WhenRowVersionIsStale()
        {
            var seeded = await AddRowVersionEntityAsync();

            await using var dbContext = new CrudMsSqlTestDbContext(_dbContextOptions);
            var repo = new RowVersionEntityRepo(dbContext, _mapper, _paginationProcessor);
            var staleToken = await repo.GetConcurrencyTokenAsync(seeded.Id);

            await UpdateRowVersionEntityDirectlyAsync(seeded.Id, "server");

            await Should.ThrowAsync<ConcurrentModificationException>(() =>
                repo.RemoveAsync(seeded.Id, staleToken)
            );
        }

        [Test]
        public async Task GetConcurrencyTokenAsync_ShouldReturnCurrentRowVersion()
        {
            var seeded = await AddRowVersionEntityAsync();

            await using var dbContext = new CrudMsSqlTestDbContext(_dbContextOptions);
            var repo = new RowVersionEntityRepo(dbContext, _mapper, _paginationProcessor);

            var token = await repo.GetConcurrencyTokenAsync(seeded.Id);

            token.ShouldBe(seeded.RowVersion);
        }

        [Test]
        public async Task GetAllDtoAsync_ShouldExposeDeletedRows_ForSoftDeleteRowVersionRepo()
        {
            var seeded = await AddSoftDeleteRowVersionEntityAsync();

            await using (var deleteContext = new CrudMsSqlTestDbContext(_dbContextOptions))
            {
                var repo = new SoftDeleteRowVersionEntityRepo(
                    deleteContext,
                    _mapper,
                    _paginationProcessor
                );
                var token = await repo.GetConcurrencyTokenAsync(seeded.Id);
                await repo.RemoveAsync(seeded.Id, token);
            }

            await using var dbContext = new CrudMsSqlTestDbContext(_dbContextOptions);
            var repoAfterDelete = new SoftDeleteRowVersionEntityRepo(
                dbContext,
                _mapper,
                _paginationProcessor
            );

            await Should.ThrowAsync<ItemNotFoundException>(() => repoAfterDelete.GetByIdAsync(seeded.Id));

            var deletedDtos = (
                await repoAfterDelete.GetAllDtoAsync(SoftDeleteQueryMode.DeletedOnly)
            ).ToList();
            deletedDtos.Count.ShouldBe(1);
            deletedDtos[0].Id.ShouldBe(seeded.Id);
            deletedDtos[0].DeletedTimestamp.ShouldNotBeNull();

            var deleted = await repoAfterDelete.GetByIdAsync(
                seeded.Id,
                SoftDeleteQueryMode.IncludeDeleted
            );
            deleted.DeletedTimestamp.ShouldNotBeNull();
        }

        [Test]
        public async Task RestoreAsync_WithExplicitToken_ShouldRestoreSoftDeletedRowVersionEntity()
        {
            var seeded = await AddSoftDeleteRowVersionEntityAsync();

            await using (var deleteContext = new CrudMsSqlTestDbContext(_dbContextOptions))
            {
                var repo = new SoftDeleteRowVersionEntityRepo(
                    deleteContext,
                    _mapper,
                    _paginationProcessor
                );
                var token = await repo.GetConcurrencyTokenAsync(seeded.Id);
                await repo.RemoveAsync(seeded.Id, token);
            }

            await using (var restoreContext = new CrudMsSqlTestDbContext(_dbContextOptions))
            {
                var repo = new SoftDeleteRowVersionEntityRepo(
                    restoreContext,
                    _mapper,
                    _paginationProcessor
                );
                var deletedToken = await repo.GetConcurrencyTokenAsync(seeded.Id);
                await repo.RestoreAsync(seeded.Id, deletedToken);
            }

            await using var verifyContext = new CrudMsSqlTestDbContext(_dbContextOptions);
            var repoAfterRestore = new SoftDeleteRowVersionEntityRepo(
                verifyContext,
                _mapper,
                _paginationProcessor
            );
            var restored = await repoAfterRestore.GetByIdAsync(seeded.Id);
            restored.DeletedTimestamp.ShouldBeNull();
        }

        [Test]
        public async Task UpdateAsync_WithExplicitToken_ShouldRegenerateGuidToken()
        {
            var seeded = await AddGuidTokenEntityAsync();

            await using var dbContext = new CrudMsSqlTestDbContext(_dbContextOptions);
            var repo = new GuidTokenEntityRepo(dbContext, _mapper, _paginationProcessor);
            var staleToken = await repo.GetConcurrencyTokenAsync(seeded.Id);

            await repo.UpdateAsync(
                new GuidTokenUpdateDto
                {
                    Id = seeded.Id,
                    Name = "updated"
                },
                staleToken
            );

            var dto = await repo.GetByIdDtoAsync(seeded.Id);
            dto.Name.ShouldBe("updated");
            dto.Etag.ShouldNotBe(staleToken);
        }

        [Test]
        public async Task UpdateAsync_WithExplicitToken_ShouldThrow_WhenGuidTokenIsStale()
        {
            var seeded = await AddGuidTokenEntityAsync();

            await using var dbContext = new CrudMsSqlTestDbContext(_dbContextOptions);
            var repo = new GuidTokenEntityRepo(dbContext, _mapper, _paginationProcessor);
            var staleToken = await repo.GetConcurrencyTokenAsync(seeded.Id);

            await UpdateGuidTokenEntityDirectlyAsync(seeded.Id, "server");

            await Should.ThrowAsync<ConcurrentModificationException>(() =>
                repo.UpdateAsync(
                    new GuidTokenUpdateDto
                    {
                        Id = seeded.Id,
                        Name = "client"
                    },
                    staleToken
                )
            );
        }

        private async Task<RowVersionEntity> AddRowVersionEntityAsync()
        {
            await using var dbContext = new CrudMsSqlTestDbContext(_dbContextOptions);
            dbContext.RowVersionEntities.Add(new RowVersionEntity { Id = 1, Name = "initial" });
            await dbContext.SaveChangesAsync();

            return await dbContext.RowVersionEntities.AsNoTracking().SingleAsync(x => x.Id == 1);
        }

        private async Task<RowVersionEntity> UpdateRowVersionEntityDirectlyAsync(int id, string name)
        {
            await using var dbContext = new CrudMsSqlTestDbContext(_dbContextOptions);
            var entity = await dbContext.RowVersionEntities.SingleAsync(x => x.Id == id);
            entity.Name = name;
            await dbContext.SaveChangesAsync();

            return await dbContext.RowVersionEntities.AsNoTracking().SingleAsync(x => x.Id == id);
        }

        private async Task<GuidTokenEntity> AddGuidTokenEntityAsync()
        {
            await using var dbContext = new CrudMsSqlTestDbContext(_dbContextOptions);
            dbContext.GuidTokenEntities.Add(new GuidTokenEntity { Id = 1, Name = "initial" });
            await dbContext.SaveChangesAsync();

            return await dbContext.GuidTokenEntities.AsNoTracking().SingleAsync(x => x.Id == 1);
        }

        private async Task<GuidTokenEntity> UpdateGuidTokenEntityDirectlyAsync(int id, string name)
        {
            await using var dbContext = new CrudMsSqlTestDbContext(_dbContextOptions);
            var entity = await dbContext.GuidTokenEntities.SingleAsync(x => x.Id == id);
            entity.Name = name;
            await dbContext.SaveChangesAsync();

            return await dbContext.GuidTokenEntities.AsNoTracking().SingleAsync(x => x.Id == id);
        }

        private async Task<SoftDeleteRowVersionEntity> AddSoftDeleteRowVersionEntityAsync()
        {
            await using var dbContext = new CrudMsSqlTestDbContext(_dbContextOptions);
            dbContext.SoftDeleteRowVersionEntities.Add(
                new SoftDeleteRowVersionEntity { Id = 1, Name = "initial" }
            );
            await dbContext.SaveChangesAsync();

            return await dbContext.SoftDeleteRowVersionEntities.AsNoTracking().SingleAsync(x =>
                x.Id == 1
            );
        }

        private static async Task EnsureGuidTokenTriggerAsync(CrudMsSqlTestDbContext dbContext)
        {
            await dbContext.Database.ExecuteSqlRawAsync(
                """
                IF OBJECT_ID('dbo.trg_crud_mssql_guid_token_entities_update', 'TR') IS NOT NULL
                    DROP TRIGGER dbo.trg_crud_mssql_guid_token_entities_update;
                """
            );

            await dbContext.Database.ExecuteSqlRawAsync(
                """
                CREATE TRIGGER dbo.trg_crud_mssql_guid_token_entities_update
                ON dbo.crud_mssql_guid_token_entities
                AFTER UPDATE
                AS
                BEGIN
                    SET NOCOUNT ON;

                    IF TRIGGER_NESTLEVEL() > 1
                        RETURN;

                    UPDATE target
                    SET etag = NEWID()
                    FROM dbo.crud_mssql_guid_token_entities AS target
                    INNER JOIN inserted AS inserted_row ON inserted_row.id = target.id;
                END;
                """
            );
        }

        private static DbContextOptions<CrudMsSqlTestDbContext> CreateDbContextOptions(
            string containerConnectionString
        )
        {
            var builder = new SqlConnectionStringBuilder(containerConnectionString)
            {
                InitialCatalog = $"crud_test_{Guid.NewGuid():N}",
            };

            return new DbContextOptionsBuilder<CrudMsSqlTestDbContext>()
                .UseSqlServer(builder.ConnectionString)
                .Options;
        }

        private sealed class CrudMsSqlTestDbContext : DbContext
        {
            public CrudMsSqlTestDbContext(DbContextOptions<CrudMsSqlTestDbContext> options)
                : base(options) { }

            public DbSet<RowVersionEntity> RowVersionEntities => Set<RowVersionEntity>();

            public DbSet<SoftDeleteRowVersionEntity> SoftDeleteRowVersionEntities =>
                Set<SoftDeleteRowVersionEntity>();

            public DbSet<GuidTokenEntity> GuidTokenEntities => Set<GuidTokenEntity>();

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                modelBuilder.Entity<RowVersionEntity>(entity =>
                {
                    entity.ToTable("crud_mssql_row_version_entities");
                    entity.HasKey(x => x.Id);
                    entity.Property(x => x.Id).ValueGeneratedNever();
                    entity.Property(x => x.RowVersion).IsRowVersion();
                });

                modelBuilder.Entity<SoftDeleteRowVersionEntity>(entity =>
                {
                    entity.ToTable("crud_mssql_soft_delete_row_version_entities");
                    entity.HasKey(x => x.Id);
                    entity.Property(x => x.Id).ValueGeneratedNever();
                    entity.Property(x => x.RowVersion).IsRowVersion();
                });

                modelBuilder.Entity<GuidTokenEntity>(entity =>
                {
                    entity.ToTable(
                        "crud_mssql_guid_token_entities",
                        tableBuilder =>
                            tableBuilder.HasTrigger("trg_crud_mssql_guid_token_entities_update")
                    );
                    entity.HasKey(x => x.Id);
                    entity.Property(x => x.Id).ValueGeneratedNever();
                    entity.Property(x => x.Etag).HasColumnName("etag").HasDefaultValueSql("NEWID()");
                });
            }
        }

        private sealed class RowVersionEntity : IEntityWithId<int>
        {
            public int Id { get; set; }

            public string Name { get; set; } = string.Empty;

            public byte[] RowVersion { get; set; } = [];
        }

        private sealed class GuidTokenEntity : IEntityWithId<int>
        {
            public int Id { get; set; }

            public string Name { get; set; } = string.Empty;

            public Guid Etag { get; set; }
        }

        private sealed class SoftDeleteRowVersionEntity : IEntityWithId<int>, IAuditableEntity
        {
            public int Id { get; set; }

            public string Name { get; set; } = string.Empty;

            public byte[] RowVersion { get; set; } = [];

            public DateTimeOffset CreatedTimestamp { get; set; }

            public DateTimeOffset? UpdatedTimestamp { get; set; }

            public DateTimeOffset? DeletedTimestamp { get; set; }
        }

        private sealed class RowVersionEntityDto : IDtoWithId<int>
        {
            public int Id { get; set; }

            public string Name { get; set; } = string.Empty;

            public byte[] RowVersion { get; set; } = [];
        }

        private sealed class RowVersionEntityInputDto : IDtoWithId<int>
        {
            public int Id { get; set; }

            public string Name { get; set; } = string.Empty;

            public byte[] RowVersion { get; set; } = [];
        }

        private sealed class GuidTokenEntityDto : IDtoWithId<int>
        {
            public int Id { get; set; }

            public string Name { get; set; } = string.Empty;

            public Guid Etag { get; set; }
        }

        private sealed class SoftDeleteRowVersionEntityDto : IDtoWithId<int>
        {
            public int Id { get; set; }

            public string Name { get; set; } = string.Empty;

            public byte[] RowVersion { get; set; } = [];

            public DateTimeOffset? DeletedTimestamp { get; set; }
        }

        private sealed class GuidTokenUpdateDto : IDtoWithId<int>
        {
            public int Id { get; set; }

            public string Name { get; set; } = string.Empty;
        }

        private sealed class RowVersionEntityRepo
            : BaseConcurrentCrudDtoRepo<
                RowVersionEntity,
                int,
                RowVersionEntityDto,
                RowVersionEntityInputDto,
                byte[]
            >
        {
            public RowVersionEntityRepo(
                DbContext dbContext,
                IMapper mapper,
                IPaginationProcessor paginationProcessor
            )
                : base(
                    dbContext,
                    mapper,
                    paginationProcessor,
                    CrudConcurrencyConfiguration.SqlServerRowVersion(
                        nameof(RowVersionEntity.RowVersion)
                    )
                ) { }
        }

        private sealed class GuidTokenEntityRepo
            : BaseConcurrentCrudDtoRepo<
                GuidTokenEntity,
                int,
                GuidTokenEntityDto,
                GuidTokenUpdateDto,
                Guid
            >
        {
            public GuidTokenEntityRepo(
                DbContext dbContext,
                IMapper mapper,
                IPaginationProcessor paginationProcessor
            )
                : base(
                    dbContext,
                    mapper,
                    paginationProcessor,
                    CrudConcurrencyConfiguration.ForProperty<Guid>(nameof(GuidTokenEntity.Etag))
                ) { }
        }

        private sealed class SoftDeleteRowVersionEntityRepo
            : BaseConcurrentSoftDeleteCrudDtoRepo<
                SoftDeleteRowVersionEntity,
                int,
                SoftDeleteRowVersionEntityDto,
                byte[]
            >
        {
            public SoftDeleteRowVersionEntityRepo(
                DbContext dbContext,
                IMapper mapper,
                IPaginationProcessor paginationProcessor
            )
                : base(
                    dbContext,
                    mapper,
                    paginationProcessor,
                    CrudConcurrencyConfiguration.SqlServerRowVersion(
                        nameof(SoftDeleteRowVersionEntity.RowVersion)
                    )
                ) { }
        }
    }
}
