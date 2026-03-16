using Microsoft.EntityFrameworkCore;

namespace tools_dotnet.Tests
{
    internal class PostgresContainerTestDbContext : DbContext
    {
        public PostgresContainerTestDbContext(DbContextOptions options) : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Removes the configuration for postgres for testing
        }

        public static void SetupTestDb(out DbContextOptions options, string connectionString)
        {
            var databaseName = connectionString.Split("Database=")[1].Split(";")[0];
            connectionString = connectionString.Replace($"Database={databaseName}", $"Database={Guid.CreateVersion7()}");

            var optionsBuilder = new DbContextOptionsBuilder<PostgresContainerTestDbContext>()
                .UseNpgsql(connectionString);

            options = optionsBuilder.Options;

            using (var context = new PostgresContainerTestDbContext(options))
            {
                context.Database.EnsureCreated();
            }
        }
    }
}