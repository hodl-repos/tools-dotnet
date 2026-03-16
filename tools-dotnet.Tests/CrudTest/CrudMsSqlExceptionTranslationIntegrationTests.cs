using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Data.SqlClient;
using Shouldly;
using Testcontainers.MsSql;
using tools_dotnet.Dao.Crud.Impl;
using tools_dotnet.Dao.Entity;
using tools_dotnet.Exceptions;
using tools_dotnet.Pagination.Services;

namespace tools_dotnet.Tests.CrudTest
{
    [TestFixture]
    public class CrudMsSqlExceptionTranslationIntegrationTests
    {
        private readonly MsSqlContainer _msSqlContainer = new MsSqlBuilder(
            "mcr.microsoft.com/mssql/server:2022-latest"
        )
            .WithPassword("Your_strong_password123!")
            .WithCleanUp(true)
            .Build();

        private DbContextOptions<CrudMsSqlExceptionDbContext> _dbContextOptions = null!;
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

            var mapperConfiguration = new MapperConfiguration(_ => { }, NullLoggerFactory.Instance);
            _mapper = mapperConfiguration.CreateMapper();
            _paginationProcessor = new PaginationProcessor();

            await using var dbContext = new CrudMsSqlExceptionDbContext(_dbContextOptions);
            await dbContext.Database.EnsureCreatedAsync();
            await EnsureUnknownDeleteTriggerAsync(dbContext);
        }

        [Test]
        public async Task AddAsync_ShouldThrowConflictingItemException_WhenUniqueConstraintIsViolated()
        {
            await SeedParentAsync(1, "duplicate-code");

            await using var dbContext = new CrudMsSqlExceptionDbContext(_dbContextOptions);
            var repo = new ParentRepo(dbContext, _mapper, _paginationProcessor);

            await Should.ThrowAsync<ConflictingItemException>(() =>
                repo.AddAsync(new ParentEntity { Id = 2, Code = "duplicate-code" })
            );
        }

        [Test]
        public async Task RemoveAsync_ShouldThrowDependentItemException_WhenForeignKeyBlocksDelete()
        {
            await SeedParentAsync(1, "parent");
            await SeedChildAsync(1, 1);

            await using var dbContext = new CrudMsSqlExceptionDbContext(_dbContextOptions);
            var repo = new ParentRepo(dbContext, _mapper, _paginationProcessor);

            var exception = await Should.ThrowAsync<DependentItemException>(() => repo.RemoveAsync(1));
            exception.OnRemove.ShouldBeTrue();
        }

        [Test]
        public async Task RemoveAsync_ShouldSoftDeleteAuditableEntity_WithoutAttachConflict()
        {
            await SeedSoftDeleteParentAsync(1, "soft-delete");

            await using var dbContext = new CrudMsSqlExceptionDbContext(_dbContextOptions);
            var repo = new SoftDeleteParentRepo(dbContext, _mapper, _paginationProcessor);

            var tracked = await repo.GetByIdAsync(1, SoftDeleteQueryMode.IncludeDeleted);

            await repo.RemoveAsync(1);

            dbContext.ChangeTracker.Entries<SoftDeleteParentEntity>().Count().ShouldBe(1);

            await Should.ThrowAsync<ItemNotFoundException>(() => repo.GetByIdAsync(1));

            var deleted = await repo.GetByIdAsync(1, SoftDeleteQueryMode.IncludeDeleted);
            deleted.ShouldBeSameAs(tracked);
            deleted.DeletedTimestamp.ShouldNotBeNull();
            deleted.CreatedTimestamp.ShouldBe(tracked.CreatedTimestamp);
        }

        [Test]
        public async Task RemoveAsync_ShouldHardDeleteNonAuditableEntity_WhenEntityDoesNotSupportSoftDelete()
        {
            await SeedParentAsync(1, "delete-me");

            await using var dbContext = new CrudMsSqlExceptionDbContext(_dbContextOptions);
            var repo = new ParentRepo(dbContext, _mapper, _paginationProcessor);

            await repo.RemoveAsync(1);

            (await dbContext.Parents.AsNoTracking().AnyAsync(x => x.Id == 1)).ShouldBeFalse();
        }

        [Test]
        public async Task RemoveAsync_ShouldRethrowRawDbUpdateException_WhenProviderErrorIsUnknown()
        {
            await SeedTriggerDeleteEntityAsync(1, "trigger-delete");

            await using var dbContext = new CrudMsSqlExceptionDbContext(_dbContextOptions);
            var repo = new TriggerDeleteRepo(dbContext, _mapper, _paginationProcessor);

            var exception = await Should.ThrowAsync<DbUpdateException>(() => repo.RemoveAsync(1));
            exception.ShouldNotBeOfType<DependentItemException>();
        }

        private async Task SeedParentAsync(int id, string code)
        {
            await using var dbContext = new CrudMsSqlExceptionDbContext(_dbContextOptions);
            dbContext.Parents.Add(new ParentEntity { Id = id, Code = code });
            await dbContext.SaveChangesAsync();
        }

        private async Task SeedChildAsync(int id, int parentId)
        {
            await using var dbContext = new CrudMsSqlExceptionDbContext(_dbContextOptions);
            dbContext.Children.Add(new ChildEntity { Id = id, ParentId = parentId });
            await dbContext.SaveChangesAsync();
        }

        private async Task SeedSoftDeleteParentAsync(int id, string code)
        {
            await using var dbContext = new CrudMsSqlExceptionDbContext(_dbContextOptions);
            dbContext.SoftDeleteParents.Add(
                new SoftDeleteParentEntity
                {
                    Id = id,
                    Code = code,
                    CreatedTimestamp = DateTimeOffset.UtcNow
                }
            );
            await dbContext.SaveChangesAsync();
        }

        private async Task SeedTriggerDeleteEntityAsync(int id, string code)
        {
            await using var dbContext = new CrudMsSqlExceptionDbContext(_dbContextOptions);
            dbContext.TriggerDeleteEntities.Add(new TriggerDeleteEntity { Id = id, Code = code });
            await dbContext.SaveChangesAsync();
        }

        private static async Task EnsureUnknownDeleteTriggerAsync(CrudMsSqlExceptionDbContext dbContext)
        {
            await dbContext.Database.ExecuteSqlRawAsync(
                """
                IF OBJECT_ID('dbo.trg_crud_mssql_exception_trigger_delete_entities_block_delete', 'TR') IS NOT NULL
                    DROP TRIGGER dbo.trg_crud_mssql_exception_trigger_delete_entities_block_delete;
                """
            );

            await dbContext.Database.ExecuteSqlRawAsync(
                """
                CREATE TRIGGER dbo.trg_crud_mssql_exception_trigger_delete_entities_block_delete
                ON dbo.crud_mssql_exception_trigger_delete_entities
                INSTEAD OF DELETE
                AS
                BEGIN
                    THROW 50000, 'Delete blocked by trigger.', 1;
                END;
                """
            );
        }

        private static DbContextOptions<CrudMsSqlExceptionDbContext> CreateDbContextOptions(
            string containerConnectionString
        )
        {
            var builder = new SqlConnectionStringBuilder(containerConnectionString)
            {
                InitialCatalog = $"crud_exception_test_{Guid.NewGuid():N}",
            };

            return new DbContextOptionsBuilder<CrudMsSqlExceptionDbContext>()
                .UseSqlServer(builder.ConnectionString)
                .Options;
        }

        private sealed class CrudMsSqlExceptionDbContext : DbContext
        {
            public CrudMsSqlExceptionDbContext(DbContextOptions<CrudMsSqlExceptionDbContext> options)
                : base(options) { }

            public DbSet<ParentEntity> Parents => Set<ParentEntity>();

            public DbSet<ChildEntity> Children => Set<ChildEntity>();

            public DbSet<SoftDeleteParentEntity> SoftDeleteParents => Set<SoftDeleteParentEntity>();

            public DbSet<TriggerDeleteEntity> TriggerDeleteEntities => Set<TriggerDeleteEntity>();

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                modelBuilder.Entity<ParentEntity>(entityBuilder =>
                {
                    entityBuilder.ToTable("crud_mssql_exception_parents");
                    entityBuilder.HasKey(x => x.Id);
                    entityBuilder.Property(x => x.Id).ValueGeneratedNever();
                    entityBuilder.HasIndex(x => x.Code).IsUnique();
                    entityBuilder.Property(x => x.Code).HasMaxLength(100).IsRequired();
                });

                modelBuilder.Entity<ChildEntity>(entityBuilder =>
                {
                    entityBuilder.ToTable("crud_mssql_exception_children");
                    entityBuilder.HasKey(x => x.Id);
                    entityBuilder.Property(x => x.Id).ValueGeneratedNever();
                    entityBuilder
                        .HasOne(x => x.Parent)
                        .WithMany(x => x.Children)
                        .HasForeignKey(x => x.ParentId)
                        .OnDelete(DeleteBehavior.Restrict);
                });

                modelBuilder.Entity<SoftDeleteParentEntity>(entityBuilder =>
                {
                    entityBuilder.ToTable("crud_mssql_exception_soft_delete_parents");
                    entityBuilder.HasKey(x => x.Id);
                    entityBuilder.Property(x => x.Id).ValueGeneratedNever();
                    entityBuilder.Property(x => x.Code).HasMaxLength(100).IsRequired();
                    entityBuilder.Property(x => x.CreatedTimestamp).IsRequired();
                });

                modelBuilder.Entity<TriggerDeleteEntity>(entityBuilder =>
                {
                    entityBuilder.ToTable(
                        "crud_mssql_exception_trigger_delete_entities",
                        tableBuilder =>
                            tableBuilder.HasTrigger(
                                "trg_crud_mssql_exception_trigger_delete_entities_block_delete"
                            )
                    );
                    entityBuilder.HasKey(x => x.Id);
                    entityBuilder.Property(x => x.Id).ValueGeneratedNever();
                    entityBuilder.Property(x => x.Code).HasMaxLength(100).IsRequired();
                });
            }
        }

        private sealed class ParentEntity : IEntityWithId<int>
        {
            public int Id { get; set; }

            public string Code { get; set; } = string.Empty;

            public List<ChildEntity> Children { get; set; } = [];
        }

        private sealed class ChildEntity : IEntityWithId<int>
        {
            public int Id { get; set; }

            public int ParentId { get; set; }

            public ParentEntity Parent { get; set; } = null!;
        }

        private sealed class SoftDeleteParentEntity : IEntityWithId<int>, IAuditableEntity
        {
            public int Id { get; set; }

            public string Code { get; set; } = string.Empty;

            public DateTimeOffset CreatedTimestamp { get; set; }

            public DateTimeOffset? UpdatedTimestamp { get; set; }

            public DateTimeOffset? DeletedTimestamp { get; set; }
        }

        private sealed class TriggerDeleteEntity : IEntityWithId<int>
        {
            public int Id { get; set; }

            public string Code { get; set; } = string.Empty;
        }

        private sealed class ParentRepo : BaseCrudRepo<ParentEntity, int>
        {
            public ParentRepo(
                DbContext dbContext,
                IMapper mapper,
                IPaginationProcessor paginationProcessor
            )
                : base(dbContext, mapper, paginationProcessor) { }
        }

        private sealed class SoftDeleteParentRepo : BaseCrudRepo<SoftDeleteParentEntity, int>
        {
            public SoftDeleteParentRepo(
                DbContext dbContext,
                IMapper mapper,
                IPaginationProcessor paginationProcessor
            )
                : base(dbContext, mapper, paginationProcessor) { }
        }

        private sealed class TriggerDeleteRepo : BaseCrudRepo<TriggerDeleteEntity, int>
        {
            public TriggerDeleteRepo(
                DbContext dbContext,
                IMapper mapper,
                IPaginationProcessor paginationProcessor
            )
                : base(dbContext, mapper, paginationProcessor) { }
        }
    }
}
