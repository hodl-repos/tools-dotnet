using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Update;
using Shouldly;
using tools_dotnet.Dao.Crud;
using tools_dotnet.Dao.Crud.Impl;
using tools_dotnet.Dao.Entity;
using tools_dotnet.Dao.Interceptors;
using tools_dotnet.Errors;
using tools_dotnet.Exceptions;
using tools_dotnet.Utility;

namespace tools_dotnet.Tests.CrudTest
{
    [TestFixture]
    public class CrudConcurrencyInfrastructureTests
    {
        private DbContextOptions<CrudConcurrencyInfrastructureDbContext> _dbContextOptions = null!;

        [SetUp]
        public void Setup()
        {
            _dbContextOptions = new DbContextOptionsBuilder<CrudConcurrencyInfrastructureDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
                .AddInterceptors(new TimestampsInterceptor())
                .Options;
        }

        [Test]
        public void MapExceptionToApiError_ShouldReturnConcurrentModificationError()
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Path = "/tracked/1";

            var error = httpContext.MapExceptionToApiError(
                new ConcurrentModificationException("db-stamp", "request-stamp")
            );

            error.ShouldBeOfType<ApiConcurrentModificationError>();
            error!.Status.ShouldBe(StatusCodes.Status409Conflict);
            error.Extensions["dbConcurrencyStamp"].ShouldBe("db-stamp");
            error.Extensions["requestConcurrencyStamp"].ShouldBe("request-stamp");
        }

        [Test]
        public async Task CreateConcurrentModificationExceptionAsync_ShouldReadDatabaseTimestamp()
        {
            var seeded = await AddTrackedEntityAsync();

            await using var staleContext = CreateDbContext();
            var staleEntity = await staleContext.TrackedEntities.SingleAsync(x => x.Id == seeded.Id);
            var requestUpdatedTimestamp = staleEntity.UpdatedTimestamp;

            var externallyUpdated = await UpdateTrackedEntityDirectlyAsync(seeded.Id, "server");
            staleEntity.Name = "client";

            #pragma warning disable EF1001
            IUpdateEntry updateEntry = staleContext.Entry(staleEntity).GetInfrastructure();
            #pragma warning restore EF1001
            var dbUpdateConcurrencyException = new DbUpdateConcurrencyException(
                "Simulated conflict",
                new[] { updateEntry }
            );

            var translated = await CrudConcurrencyHelper.CreateConcurrentModificationExceptionAsync(
                CrudConcurrencyConfiguration.UpdatedTimestamp(),
                dbUpdateConcurrencyException,
                requestUpdatedTimestamp
            );

            translated.DbConcurrencyStamp.ShouldBe(
                externallyUpdated.UpdatedTimestamp?.ToString("O")
            );
            translated.RequestConcurrencyStamp.ShouldBe(
                requestUpdatedTimestamp?.ToString("O")
            );
        }

        private CrudConcurrencyInfrastructureDbContext CreateDbContext()
        {
            return new CrudConcurrencyInfrastructureDbContext(_dbContextOptions);
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

        private sealed class CrudConcurrencyInfrastructureDbContext : DbContext
        {
            public CrudConcurrencyInfrastructureDbContext(
                DbContextOptions<CrudConcurrencyInfrastructureDbContext> options
            )
                : base(options) { }

            public DbSet<TrackedEntity> TrackedEntities => Set<TrackedEntity>();
        }

        private sealed class TrackedEntity : IEntityWithId<int>, IChangeTrackingEntity
        {
            public int Id { get; set; }

            public string Name { get; set; } = string.Empty;

            public DateTimeOffset CreatedTimestamp { get; set; }

            public DateTimeOffset? UpdatedTimestamp { get; set; }
        }
    }
}
