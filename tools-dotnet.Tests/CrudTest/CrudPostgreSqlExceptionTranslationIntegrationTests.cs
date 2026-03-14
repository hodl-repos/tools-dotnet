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

        private sealed class CrudPostgreSqlExceptionDbContext : DbContext
        {
            public CrudPostgreSqlExceptionDbContext(
                DbContextOptions<CrudPostgreSqlExceptionDbContext> options
            )
                : base(options) { }

            public DbSet<ParentEntity> Parents => Set<ParentEntity>();

            public DbSet<ChildEntity> Children => Set<ChildEntity>();

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

        private sealed class ParentRepo : BaseCrudRepo<ParentEntity, int>
        {
            public ParentRepo(
                DbContext dbContext,
                IMapper mapper,
                IPaginationProcessor paginationProcessor
            )
                : base(dbContext, mapper, paginationProcessor) { }
        }
    }
}
