using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Shouldly;
using Swashbuckle.AspNetCore.Swagger;
using tools_dotnet.Paging;
using tools_dotnet.Paging.Impl;
using tools_dotnet.Pagination.Attributes;
using tools_dotnet.Pagination.OpenApi;
using Microsoft.OpenApi;

namespace tools_dotnet.Tests.PaginationTest
{
    [ApiController]
    [Route("openapi-pagination-tests")]
    public sealed class PaginationOpenApiTestController : ControllerBase
    {
        [HttpGet("inferred")]
        public ActionResult<IPagedList<PaginationOpenApiFilterModel>> GetInferred([FromQuery] ApiPagination pagination)
        {
            throw new System.NotImplementedException();
        }

        [HttpGet("explicit")]
        [PaginationOpenApiType(typeof(PaginationOpenApiFilterModel))]
        public ActionResult<IReadOnlyList<PaginationOpenApiFilterModel>> GetExplicit([FromQuery] ApiPagination pagination)
        {
            throw new System.NotImplementedException();
        }

        [HttpGet("nested")]
        [PaginationOpenApiType(typeof(PaginationOpenApiNestedModel))]
        public ActionResult<IReadOnlyList<PaginationOpenApiNestedModel>> GetNested([FromQuery] ApiPagination pagination)
        {
            throw new System.NotImplementedException();
        }
    }

    public enum PaginationOpenApiStatus
    {
        Draft = 0,
        Active = 1
    }

    public sealed class PaginationOpenApiFilterModel
    {
        [Pagination(Name = "name", CanFilter = true, CanSort = true)]
        public string Name { get; set; } = string.Empty;

        [Pagination(Name = "age", CanFilter = true, CanSort = true)]
        public int Age { get; set; }

        [Pagination(Name = "enabled", CanFilter = true, CanSort = false)]
        public bool Enabled { get; set; }

        [Pagination(Name = "status", CanFilter = true, CanSort = true)]
        public PaginationOpenApiStatus Status { get; set; }

        [Pagination(Name = "external_id", CanFilter = true, CanSort = false)]
        public System.Guid ExternalId { get; set; }

        [Pagination(Name = "created_at", CanFilter = true, CanSort = true)]
        public System.DateTimeOffset? CreatedAt { get; set; }

        public string NotDocumented { get; set; } = string.Empty;
    }

    public sealed class PaginationOpenApiNestedModel
    {
        [Pagination(Name = "owner", CanFilter = false, CanSort = false, CanFilterSubProperties = true, CanSortSubProperties = true)]
        public PaginationOpenApiNestedOwner Owner { get; set; } = new();

        [Pagination(Name = "blocked_owner", CanFilter = false, CanSort = false, CanFilterSubProperties = false, CanSortSubProperties = false)]
        public PaginationOpenApiNestedOwner BlockedOwner { get; set; } = new();
    }

    public sealed class PaginationOpenApiNestedOwner
    {
        [Pagination(Name = "display_name", CanFilter = true, CanSort = true)]
        public string DisplayName { get; set; } = string.Empty;

        [Pagination(Name = "age", CanFilter = true, CanSort = false)]
        public int Age { get; set; }
    }

    [TestFixture]
    public class PaginationOpenApiOperationFilterTests
    {
        [Test]
        public void Swagger_ShouldDocumentFiltersAndSorts_FromReturnType()
        {
            var operation = GetOperation("/openapi-pagination-tests/inferred");
            var filtersParameter = GetQueryParameter(operation, "filters");
            var sortsParameter = GetQueryParameter(operation, "sorts");

            filtersParameter.Description.ShouldNotBeNull();
            filtersParameter.Description.ShouldContain("Allowed filter fields:");
            filtersParameter.Description.ShouldContain("`name` (string): ==, ==*, !=, !=*, @=, @=*, !@=, !@=*, _=, _=*, !_=, !_=*, _-=, _-=*, !_-=, !_-=*");
            filtersParameter.Description.ShouldContain("`age` (number): ==, !=, >, >=, <, <=");
            filtersParameter.Description.ShouldContain("`enabled` (bool): ==, !=");
            filtersParameter.Description.ShouldContain("`status` (enum(PaginationOpenApiStatus)): ==, !=");
            filtersParameter.Description.ShouldContain("`external_id` (guid): ==, !=");
            filtersParameter.Description.ShouldContain("`created_at` (date?): ==, !=, >, >=, <, <=");
            filtersParameter.Description.ShouldNotContain("NotDocumented");

            sortsParameter.Description.ShouldNotBeNull();
            sortsParameter.Description.ShouldContain("Allowed sort fields:");
            sortsParameter.Description.ShouldContain("`name`");
            sortsParameter.Description.ShouldContain("`age`");
            sortsParameter.Description.ShouldContain("`status`");
            sortsParameter.Description.ShouldContain("`created_at`");
            sortsParameter.Description.ShouldNotContain("`enabled`");
            sortsParameter.Description.ShouldNotContain("`external_id`");
        }

        [Test]
        public void Swagger_ShouldDocumentFilters_FromExplicitPaginationOpenApiTypeAttribute()
        {
            var operation = GetOperation("/openapi-pagination-tests/explicit");
            var filtersParameter = GetQueryParameter(operation, "filters");

            filtersParameter.Description.ShouldNotBeNull();
            filtersParameter.Description.ShouldContain("Allowed filter fields:");
            filtersParameter.Description.ShouldContain("`name` (string):");
            filtersParameter.Description.ShouldContain("`age` (number):");
        }

        [Test]
        public void Swagger_ShouldDocumentNestedFields_WhenSubPropertiesAreEnabled()
        {
            var operation = GetOperation("/openapi-pagination-tests/nested");
            var filtersParameter = GetQueryParameter(operation, "filters");
            var sortsParameter = GetQueryParameter(operation, "sorts");

            filtersParameter.Description.ShouldNotBeNull();
            filtersParameter.Description.ShouldContain("`owner.display_name` (string):");
            filtersParameter.Description.ShouldContain("`owner.age` (number):");
            filtersParameter.Description.ShouldNotContain("`blocked_owner.display_name`");

            sortsParameter.Description.ShouldNotBeNull();
            sortsParameter.Description.ShouldContain("`owner.display_name`");
            sortsParameter.Description.ShouldNotContain("`owner.age`");
            sortsParameter.Description.ShouldNotContain("`blocked_owner.display_name`");
        }

        private static OpenApiOperation GetOperation(string path)
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<IWebHostEnvironment>(new TestWebHostEnvironment());
            services.AddSingleton<IHostEnvironment>(x => x.GetRequiredService<IWebHostEnvironment>());

            services
                .AddControllers()
                .PartManager
                .ApplicationParts
                .Add(new AssemblyPart(typeof(PaginationOpenApiOperationFilterTests).Assembly));

            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo { Title = "Pagination OpenAPI Tests", Version = "v1" });
                options.AddPaginationOpenApiSupport();
            });

            using var serviceProvider = services.BuildServiceProvider();
            var swaggerProvider = serviceProvider.GetRequiredService<ISwaggerProvider>();
            var document = swaggerProvider.GetSwagger("v1");
            return document.Paths[path].Operations![HttpMethod.Get];
        }

        private static IOpenApiParameter GetQueryParameter(OpenApiOperation operation, string name)
        {
            return operation.Parameters!.Single(x =>
                x.In == ParameterLocation.Query &&
                string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
        }

        private sealed class TestWebHostEnvironment : IWebHostEnvironment
        {
            public string ApplicationName { get; set; } = "tools-dotnet.Tests";

            public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();

            public string ContentRootPath { get; set; } = System.AppContext.BaseDirectory;

            public string EnvironmentName { get; set; } = Environments.Development;

            public string WebRootPath { get; set; } = System.AppContext.BaseDirectory;

            public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        }
    }
}