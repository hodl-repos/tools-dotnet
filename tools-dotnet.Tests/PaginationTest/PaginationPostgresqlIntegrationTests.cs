using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace tools_dotnet.Tests.PaginationTest
{
    [TestFixture]
    public class PaginationPostgresqlIntegrationTests
    {
        private readonly PostgreSqlContainer _postgreSqlContainer =
            new PostgreSqlBuilder("postgres:18")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .WithDatabase("test_db")
            .WithCleanUp(true)
            .Build();

        private DbContextOptions _dbContextOptions;

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
            PostgresContainerTestDbContext.SetupTestDb(
                out _dbContextOptions,
                _postgreSqlContainer.GetConnectionString()
            );

            using (var dbContext = new PostgresContainerTestDbContext(_dbContextOptions))
            {
                await dbContext.SaveChangesAsync();
            }
        }

        [TestCase]
        public async Task TestGetRepo()
        {
        }
    }
}