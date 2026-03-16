using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Testcontainers.PostgreSql;
using tools_dotnet.Dao.Crud.Impl;
using tools_dotnet.Dao.Entity;
using tools_dotnet.Exceptions;
using tools_dotnet.Pagination.Services;

namespace tools_dotnet.Tests.CrudTest
{
    [TestFixture]
    public class CrudPostgreSqlExceptionTranslationIntegrationTests
    {
        private readonly PostgreSqlContainer _postgreSqlContainer = new PostgreSqlBuilder(
            "postgres:18-alpine"
        )
            .WithUsername("postgres")
            .WithPassword("postgres")
            .WithDatabase("test_db")
            .WithCleanUp(true)
            .Build();

        private DbContextOptions<CrudPostgreSqlExceptionDbContext> _dbContextOptions = null!;
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
            _dbContextOptions = new DbContextOptionsBuilder<CrudPostgreSqlExceptionDbContext>()
                .UseNpgsql(_postgreSqlContainer.GetConnectionString())
                .Options;

            var mapperConfiguration = new MapperConfiguration(_ => { }, NullLoggerFactory.Instance);
            _mapper = mapperConfiguration.CreateMapper();
            _paginationProcessor = new PaginationProcessor();

            await using var dbContext = new CrudPostgreSqlExceptionDbContext(_dbContextOptions);
            await dbContext.Database.EnsureDeletedAsync();
            await dbContext.Database.EnsureCreatedAsync();
            await EnsureUnknownDeleteTriggerAsync(dbContext);
        }

        [Test]
        public async Task AddAsync_ShouldThrowConflictingItemException_WhenUniqueConstraintIsViolated()
        {
            await SeedParentAsync(1, "duplicate-code");

            await using var dbContext = new CrudPostgreSqlExceptionDbContext(_dbContextOptions);
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

            await using var dbContext = new CrudPostgreSqlExceptionDbContext(_dbContextOptions);
            var repo = new ParentRepo(dbContext, _mapper, _paginationProcessor);

            var exception = await Should.ThrowAsync<DependentItemException>(() => repo.RemoveAsync(1));
            exception.OnRemove.ShouldBeTrue();
        }

        [Test]
        public async Task RemoveAsync_ShouldSoftDeleteAuditableEntity_WithoutAttachConflict()
        {
            await SeedSoftDeleteParentAsync(1, "soft-delete");

            await using var dbContext = new CrudPostgreSqlExceptionDbContext(_dbContextOptions);
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

            await using var dbContext = new CrudPostgreSqlExceptionDbContext(_dbContextOptions);
            var repo = new ParentRepo(dbContext, _mapper, _paginationProcessor);

            await repo.RemoveAsync(1);

            (await dbContext.Parents.AsNoTracking().AnyAsync(x => x.Id == 1)).ShouldBeFalse();
        }

        [Test]
        public async Task RemoveAsync_ShouldRethrowRawDbUpdateException_WhenProviderErrorIsUnknown()
        {
            await SeedTriggerDeleteEntityAsync(1, "trigger-delete");

            await using var dbContext = new CrudPostgreSqlExceptionDbContext(_dbContextOptions);
            var repo = new TriggerDeleteRepo(dbContext, _mapper, _paginationProcessor);

            var exception = await Should.ThrowAsync<DbUpdateException>(() => repo.RemoveAsync(1));
            exception.ShouldNotBeOfType<DependentItemException>();
        }

        private async Task SeedParentAsync(int id, string code)
        {
            await using var dbContext = new CrudPostgreSqlExceptionDbContext(_dbContextOptions);
            dbContext.Parents.Add(new ParentEntity { Id = id, Code = code });
            await dbContext.SaveChangesAsync();
        }

        private async Task SeedChildAsync(int id, int parentId)
        {
            await using var dbContext = new CrudPostgreSqlExceptionDbContext(_dbContextOptions);
            dbContext.Children.Add(new ChildEntity { Id = id, ParentId = parentId });
            await dbContext.SaveChangesAsync();
        }

        private async Task SeedSoftDeleteParentAsync(int id, string code)
        {
            await using var dbContext = new CrudPostgreSqlExceptionDbContext(_dbContextOptions);
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
            await using var dbContext = new CrudPostgreSqlExceptionDbContext(_dbContextOptions);
            dbContext.TriggerDeleteEntities.Add(new TriggerDeleteEntity { Id = id, Code = code });
            await dbContext.SaveChangesAsync();
        }

        private static async Task EnsureUnknownDeleteTriggerAsync(
            CrudPostgreSqlExceptionDbContext dbContext
        )
        {
            await dbContext.Database.ExecuteSqlRawAsync(
                """
                DROP TRIGGER IF EXISTS trg_crud_postgresql_exception_trigger_delete_entities_block_delete
                    ON crud_postgresql_exception_trigger_delete_entities;
                DROP FUNCTION IF EXISTS block_crud_postgresql_exception_trigger_delete();
                """
            );

            await dbContext.Database.ExecuteSqlRawAsync(
                """
                CREATE OR REPLACE FUNCTION block_crud_postgresql_exception_trigger_delete()
                RETURNS trigger
                AS $$
                BEGIN
                    RAISE EXCEPTION 'Delete blocked by trigger.' USING ERRCODE = 'P0001';
                END;
                $$ LANGUAGE plpgsql;
                """
            );

            await dbContext.Database.ExecuteSqlRawAsync(
                """
                CREATE TRIGGER trg_crud_postgresql_exception_trigger_delete_entities_block_delete
                BEFORE DELETE ON crud_postgresql_exception_trigger_delete_entities
                FOR EACH ROW
                EXECUTE FUNCTION block_crud_postgresql_exception_trigger_delete();
                """
            );
        }

        private sealed class CrudPostgreSqlExceptionDbContext : DbContext
        {
            public CrudPostgreSqlExceptionDbContext(
                DbContextOptions<CrudPostgreSqlExceptionDbContext> options
            )
                : base(options) { }

            public DbSet<ParentEntity> Parents => Set<ParentEntity>();

            public DbSet<ChildEntity> Children => Set<ChildEntity>();

            public DbSet<SoftDeleteParentEntity> SoftDeleteParents => Set<SoftDeleteParentEntity>();

            public DbSet<TriggerDeleteEntity> TriggerDeleteEntities => Set<TriggerDeleteEntity>();

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                modelBuilder.Entity<ParentEntity>(entityBuilder =>
                {
                    entityBuilder.ToTable("crud_postgresql_exception_parents");
                    entityBuilder.HasKey(x => x.Id);
                    entityBuilder.HasIndex(x => x.Code).IsUnique();
                    entityBuilder.Property(x => x.Code).HasMaxLength(100).IsRequired();
                });

                modelBuilder.Entity<ChildEntity>(entityBuilder =>
                {
                    entityBuilder.ToTable("crud_postgresql_exception_children");
                    entityBuilder.HasKey(x => x.Id);
                    entityBuilder
                        .HasOne(x => x.Parent)
                        .WithMany(x => x.Children)
                        .HasForeignKey(x => x.ParentId)
                        .OnDelete(DeleteBehavior.Restrict);
                });

                modelBuilder.Entity<SoftDeleteParentEntity>(entityBuilder =>
                {
                    entityBuilder.ToTable("crud_postgresql_exception_soft_delete_parents");
                    entityBuilder.HasKey(x => x.Id);
                    entityBuilder.Property(x => x.Id).ValueGeneratedNever();
                    entityBuilder.Property(x => x.Code).HasMaxLength(100).IsRequired();
                    entityBuilder.Property(x => x.CreatedTimestamp).IsRequired();
                });

                modelBuilder.Entity<TriggerDeleteEntity>(entityBuilder =>
                {
                    entityBuilder.ToTable("crud_postgresql_exception_trigger_delete_entities");
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

        private sealed class SoftDeleteParentRepo
            : BaseSoftDeleteCrudRepo<SoftDeleteParentEntity, int>
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
