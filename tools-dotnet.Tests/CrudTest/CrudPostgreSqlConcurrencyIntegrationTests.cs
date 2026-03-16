using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Shouldly;
using Testcontainers.PostgreSql;
using tools_dotnet.Dao.Crud;
using tools_dotnet.Dao.Crud.Impl;
using tools_dotnet.Dao.Entity;
using tools_dotnet.Dto;
using tools_dotnet.Exceptions;
using tools_dotnet.Pagination.Services;

namespace tools_dotnet.Tests.CrudTest
{
    [TestFixture]
    public class CrudPostgreSqlConcurrencyIntegrationTests
    {
        private readonly PostgreSqlContainer _postgreSqlContainer = new PostgreSqlBuilder(
            "postgres:18-alpine"
        )
            .WithUsername("postgres")
            .WithPassword("postgres")
            .WithDatabase("test_db")
            .WithCleanUp(true)
            .Build();

        private DbContextOptions<CrudPostgreSqlTestDbContext> _dbContextOptions = null!;
        private IMapper _mapper = null!;
        private PaginationProcessor _paginationProcessor = null!;

        [OneTimeSetUp]
        public async Task BeforeAllAsync()
        {
            await _postgreSqlContainer.StartAsync();
        }

        [OneTimeTearDown]
        public async Task AfterAllAsync()
        {
            await _postgreSqlContainer.DisposeAsync();
        }

        [SetUp]
        public async Task SetupRun()
        {
            _dbContextOptions = CreateDbContextOptions(_postgreSqlContainer.GetConnectionString());

            var mapperConfiguration = new MapperConfiguration(
                config =>
                {
                    config.CreateMap<XminEntity, XminEntityDto>().ReverseMap();
                    config.CreateMap<XminEntityInputDto, XminEntity>();
                    config.CreateMap<GuidTokenEntity, GuidTokenEntityDto>().ReverseMap();
                    config.CreateMap<GuidTokenUpdateDto, GuidTokenEntity>();
                },
                NullLoggerFactory.Instance
            );

            _mapper = mapperConfiguration.CreateMapper();
            _paginationProcessor = new PaginationProcessor();

            await using var dbContext = new CrudPostgreSqlTestDbContext(_dbContextOptions);
            await dbContext.Database.EnsureCreatedAsync();
            await EnsureGuidTokenTriggerAsync(dbContext);
        }

        [Test]
        public async Task UpdateAsync_ShouldThrow_WhenXminIsStale()
        {
            var seeded = await AddXminEntityAsync();
            await UpdateXminEntityDirectlyAsync(seeded.Id, "server");

            await using var dbContext = new CrudPostgreSqlTestDbContext(_dbContextOptions);
            var repo = new XminEntityRepo(dbContext, _mapper, _paginationProcessor);

            await Should.ThrowAsync<ConcurrentModificationException>(() =>
                repo.UpdateAsync(
                    new XminEntityInputDto
                    {
                        Id = seeded.Id,
                        Name = "client",
                        Xmin = seeded.Xmin
                    }
                )
            );
        }

        [Test]
        public async Task RemoveAsync_ShouldThrow_WhenXminIsStale()
        {
            var seeded = await AddXminEntityAsync();

            await using var dbContext = new CrudPostgreSqlTestDbContext(_dbContextOptions);
            var repo = new XminEntityRepo(dbContext, _mapper, _paginationProcessor);
            var staleToken = await repo.GetConcurrencyTokenAsync(seeded.Id);

            await UpdateXminEntityDirectlyAsync(seeded.Id, "server");

            await Should.ThrowAsync<ConcurrentModificationException>(() =>
                repo.RemoveAsync(seeded.Id, staleToken)
            );
        }

        [Test]
        public async Task GetConcurrencyTokenAsync_ShouldReturnCurrentXmin()
        {
            var seeded = await AddXminEntityAsync();

            await using var dbContext = new CrudPostgreSqlTestDbContext(_dbContextOptions);
            var repo = new XminEntityRepo(dbContext, _mapper, _paginationProcessor);

            var token = await repo.GetConcurrencyTokenAsync(seeded.Id);

            token.ShouldBe(seeded.Xmin);
        }

        [Test]
        public async Task UpdateAsync_WithExplicitToken_ShouldRegenerateGuidToken()
        {
            var seeded = await AddGuidTokenEntityAsync();

            await using var dbContext = new CrudPostgreSqlTestDbContext(_dbContextOptions);
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

            await using var dbContext = new CrudPostgreSqlTestDbContext(_dbContextOptions);
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

        private async Task<XminEntity> AddXminEntityAsync()
        {
            await using var dbContext = new CrudPostgreSqlTestDbContext(_dbContextOptions);
            dbContext.XminEntities.Add(new XminEntity { Id = 1, Name = "initial" });
            await dbContext.SaveChangesAsync();

            return await dbContext.XminEntities.AsNoTracking().SingleAsync(x => x.Id == 1);
        }

        private async Task<XminEntity> UpdateXminEntityDirectlyAsync(int id, string name)
        {
            await using var dbContext = new CrudPostgreSqlTestDbContext(_dbContextOptions);
            var entity = await dbContext.XminEntities.SingleAsync(x => x.Id == id);
            entity.Name = name;
            await dbContext.SaveChangesAsync();

            return await dbContext.XminEntities.AsNoTracking().SingleAsync(x => x.Id == id);
        }

        private async Task<GuidTokenEntity> AddGuidTokenEntityAsync()
        {
            await using var dbContext = new CrudPostgreSqlTestDbContext(_dbContextOptions);
            dbContext.GuidTokenEntities.Add(new GuidTokenEntity { Id = 1, Name = "initial" });
            await dbContext.SaveChangesAsync();

            return await dbContext.GuidTokenEntities.AsNoTracking().SingleAsync(x => x.Id == 1);
        }

        private async Task<GuidTokenEntity> UpdateGuidTokenEntityDirectlyAsync(int id, string name)
        {
            await using var dbContext = new CrudPostgreSqlTestDbContext(_dbContextOptions);
            var entity = await dbContext.GuidTokenEntities.SingleAsync(x => x.Id == id);
            entity.Name = name;
            await dbContext.SaveChangesAsync();

            return await dbContext.GuidTokenEntities.AsNoTracking().SingleAsync(x => x.Id == id);
        }

        private static async Task EnsureGuidTokenTriggerAsync(CrudPostgreSqlTestDbContext dbContext)
        {
            await dbContext.Database.ExecuteSqlRawAsync(
                """
                CREATE EXTENSION IF NOT EXISTS pgcrypto;
                """
            );

            await dbContext.Database.ExecuteSqlRawAsync(
                """
                DROP TRIGGER IF EXISTS trg_crud_postgresql_guid_token_entities_update
                    ON crud_postgresql_guid_token_entities;
                DROP FUNCTION IF EXISTS set_crud_postgresql_guid_token();
                """
            );

            await dbContext.Database.ExecuteSqlRawAsync(
                """
                CREATE OR REPLACE FUNCTION set_crud_postgresql_guid_token()
                RETURNS trigger
                AS $$
                BEGIN
                    NEW.etag = gen_random_uuid();
                    RETURN NEW;
                END;
                $$ LANGUAGE plpgsql;
                """
            );

            await dbContext.Database.ExecuteSqlRawAsync(
                """
                CREATE TRIGGER trg_crud_postgresql_guid_token_entities_update
                BEFORE UPDATE ON crud_postgresql_guid_token_entities
                FOR EACH ROW
                EXECUTE FUNCTION set_crud_postgresql_guid_token();
                """
            );
        }

        private static DbContextOptions<CrudPostgreSqlTestDbContext> CreateDbContextOptions(
            string containerConnectionString
        )
        {
            var builder = new NpgsqlConnectionStringBuilder(containerConnectionString)
            {
                Database = $"crud_test_{Guid.NewGuid():N}",
            };

            return new DbContextOptionsBuilder<CrudPostgreSqlTestDbContext>()
                .UseNpgsql(builder.ConnectionString)
                .Options;
        }

        private sealed class CrudPostgreSqlTestDbContext : DbContext
        {
            public CrudPostgreSqlTestDbContext(DbContextOptions<CrudPostgreSqlTestDbContext> options)
                : base(options) { }

            public DbSet<XminEntity> XminEntities => Set<XminEntity>();

            public DbSet<GuidTokenEntity> GuidTokenEntities => Set<GuidTokenEntity>();

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                modelBuilder.Entity<XminEntity>(entity =>
                {
                    entity.ToTable("crud_postgresql_xmin_entities");
                    entity.HasKey(x => x.Id);
                    entity.Property(x => x.Id).ValueGeneratedNever();
                    entity.Property(x => x.Xmin).HasColumnName("xmin").IsRowVersion();
                });

                modelBuilder.Entity<GuidTokenEntity>(entity =>
                {
                    entity.ToTable("crud_postgresql_guid_token_entities");
                    entity.HasKey(x => x.Id);
                    entity.Property(x => x.Id).ValueGeneratedNever();
                    entity.Property(x => x.Etag)
                        .HasColumnName("etag")
                        .HasDefaultValueSql("gen_random_uuid()");
                });
            }
        }

        private sealed class XminEntity : IEntityWithId<int>
        {
            public int Id { get; set; }

            public string Name { get; set; } = string.Empty;

            public uint Xmin { get; set; }
        }

        private sealed class GuidTokenEntity : IEntityWithId<int>
        {
            public int Id { get; set; }

            public string Name { get; set; } = string.Empty;

            public Guid Etag { get; set; }
        }

        private sealed class XminEntityDto : IDtoWithId<int>
        {
            public int Id { get; set; }

            public string Name { get; set; } = string.Empty;

            public uint Xmin { get; set; }
        }

        private sealed class XminEntityInputDto : IDtoWithId<int>
        {
            public int Id { get; set; }

            public string Name { get; set; } = string.Empty;

            public uint Xmin { get; set; }
        }

        private sealed class GuidTokenEntityDto : IDtoWithId<int>
        {
            public int Id { get; set; }

            public string Name { get; set; } = string.Empty;

            public Guid Etag { get; set; }
        }

        private sealed class GuidTokenUpdateDto : IDtoWithId<int>
        {
            public int Id { get; set; }

            public string Name { get; set; } = string.Empty;
        }

        private sealed class XminEntityRepo
            : BaseConcurrentCrudDtoRepo<XminEntity, int, XminEntityDto, XminEntityInputDto, uint>
        {
            public XminEntityRepo(
                DbContext dbContext,
                IMapper mapper,
                IPaginationProcessor paginationProcessor
            )
                : base(
                    dbContext,
                    mapper,
                    paginationProcessor,
                    CrudConcurrencyConfiguration.PostgreSqlXmin(nameof(XminEntity.Xmin))
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
    }
}
