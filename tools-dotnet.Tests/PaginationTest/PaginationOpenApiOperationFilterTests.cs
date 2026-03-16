using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using System.Text.Json.Nodes;
using Shouldly;
using Swashbuckle.AspNetCore.Swagger;
using tools_dotnet.Paging;
using tools_dotnet.Paging.Impl;
using tools_dotnet.Pagination.Attributes;
using tools_dotnet.Pagination.OpenApi;
using tools_dotnet.Pagination.Services;
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

    public sealed class PaginationOpenApiCustomMethods : IPaginationCustomFilterMethods, IPaginationCustomSortsMethods
    {
        public IQueryable<PaginationOpenApiFilterModel> is_adult(IQueryable<PaginationOpenApiFilterModel> source, string op, string[] values)
        {
            return source;
        }

        public IQueryable<PaginationOpenApiFilterModel> by_name_length(IQueryable<PaginationOpenApiFilterModel> source, bool useThenBy, bool desc)
        {
            return source;
        }
    }

    [TestFixture]
    public class PaginationOpenApiOperationFilterTests
    {
        [Test]
        public void Swagger_ShouldDocumentFiltersAndSorts_FromReturnType()
        {
            var operation = GetSwaggerOperation("/openapi-pagination-tests/inferred");
            var filtersParameter = GetQueryParameter(operation, "filters");
            var sortsParameter = GetQueryParameter(operation, "sorts");

            filtersParameter.Description.ShouldNotBeNull();
            filtersParameter.Description.ShouldContain("Syntax: field{operator}value.");
            filtersParameter.Description.ShouldContain("Examples:");
            filtersParameter.Description.ShouldContain("Allowed fields:");
            filtersParameter.Description.ShouldContain("`name`");
            filtersParameter.Description.ShouldContain("`age`");
            filtersParameter.Description.ShouldContain("`enabled`");
            filtersParameter.Description.ShouldContain("`status`");
            filtersParameter.Description.ShouldContain("`external_id`");
            filtersParameter.Description.ShouldContain("`created_at`");
            filtersParameter.Description.ShouldNotContain("NotDocumented");

            sortsParameter.Description.ShouldNotBeNull();
            sortsParameter.Description.ShouldContain("Syntax: field for ascending");
            sortsParameter.Description.ShouldContain("Examples:");
            sortsParameter.Description.ShouldContain("Allowed fields:");
            sortsParameter.Description.ShouldContain("`name`");
            sortsParameter.Description.ShouldContain("`age`");
            sortsParameter.Description.ShouldContain("`status`");
            sortsParameter.Description.ShouldContain("`created_at`");
            sortsParameter.Description.ShouldNotContain("`enabled`");
            sortsParameter.Description.ShouldNotContain("`external_id`");

            var filtersExtension = GetPaginationExtension(filtersParameter);
            filtersExtension["mode"]?.GetValue<string>().ShouldBe("filters");
            filtersExtension["examples"]?.AsArray().Select(GetJsonNodeString).ShouldBe(
                ["name==sample", "age>=42", "name==sample,enabled==true"],
                ignoreOrder: false
            );

            var filterFields = filtersExtension["fields"]?.AsArray().ShouldNotBeNull();
            filterFields.Count.ShouldBe(6);
            filterFields[0]?["name"]?.GetValue<string>().ShouldBe("age");
            filterFields[0]?["type"]?.GetValue<string>().ShouldBe("number");
            filterFields[0]?["source"]?.GetValue<string>().ShouldBe("member");
            filterFields[0]?["operators"]?.AsArray().Select(GetJsonNodeString).ShouldContain(">=");

            var sortsExtension = GetPaginationExtension(sortsParameter);
            sortsExtension["mode"]?.GetValue<string>().ShouldBe("sorts");
            sortsExtension["examples"]?.AsArray().Select(GetJsonNodeString).ShouldBe(
                ["name", "-created_at", "status,-created_at"],
                ignoreOrder: false
            );
            sortsExtension["fields"]?.AsArray().Any(x =>
                string.Equals(
                    x?["name"]?.GetValue<string>(),
                    "enabled",
                    StringComparison.Ordinal
                )
            ).ShouldBeFalse();

            GetPrimaryExampleValue(filtersParameter).ShouldBe("name==sample");
            GetExampleValues(filtersParameter).ShouldContainKeyAndValue("comparison", "age>=42");
            GetExampleValues(sortsParameter).ShouldContainKeyAndValue("descending", "-created_at");
        }

        [Test]
        public void Swagger_ShouldDocumentFilters_FromExplicitPaginationOpenApiTypeAttribute()
        {
            var operation = GetSwaggerOperation("/openapi-pagination-tests/explicit");
            var filtersParameter = GetQueryParameter(operation, "filters");

            filtersParameter.Description.ShouldNotBeNull();
            filtersParameter.Description.ShouldContain("Allowed fields:");
            filtersParameter.Description.ShouldContain("`name`");
            filtersParameter.Description.ShouldContain("`age`");
        }

        [Test]
        public void Swagger_ShouldDocumentNestedFields_WhenSubPropertiesAreEnabled()
        {
            var operation = GetSwaggerOperation("/openapi-pagination-tests/nested");
            var filtersParameter = GetQueryParameter(operation, "filters");
            var sortsParameter = GetQueryParameter(operation, "sorts");

            filtersParameter.Description.ShouldNotBeNull();
            filtersParameter.Description.ShouldContain("`owner.display_name`");
            filtersParameter.Description.ShouldContain("`owner.age`");
            filtersParameter.Description.ShouldNotContain("`blocked_owner.display_name`");

            sortsParameter.Description.ShouldNotBeNull();
            sortsParameter.Description.ShouldContain("`owner.display_name`");
            sortsParameter.Description.ShouldNotContain("`owner.age`");
            sortsParameter.Description.ShouldNotContain("`blocked_owner.display_name`");

            var filtersExtension = GetPaginationExtension(filtersParameter);
            filtersExtension["fields"]?.AsArray().Any(x =>
                string.Equals(
                    x?["name"]?.GetValue<string>(),
                    "owner.display_name",
                    StringComparison.Ordinal
                )
            ).ShouldBeTrue();
            filtersExtension["fields"]?.AsArray().Any(x =>
                string.Equals(
                    x?["name"]?.GetValue<string>(),
                    "blocked_owner.display_name",
                    StringComparison.Ordinal
                )
            ).ShouldBeFalse();
        }

        [Test]
        public void Swagger_ShouldDocumentCustomFilterAndSortMethods_WhenRegistered()
        {
            var operation = GetSwaggerOperation("/openapi-pagination-tests/inferred", includeCustomMethods: true);
            var filtersParameter = GetQueryParameter(operation, "filters");
            var sortsParameter = GetQueryParameter(operation, "sorts");

            filtersParameter.Description.ShouldNotBeNull();
            filtersParameter.Description.ShouldContain("`is_adult`");

            sortsParameter.Description.ShouldNotBeNull();
            sortsParameter.Description.ShouldContain("`by_name_length`");

            var filtersExtension = GetPaginationExtension(filtersParameter);
            filtersExtension["fields"]?.AsArray().Any(x =>
                string.Equals(x?["name"]?.GetValue<string>(), "is_adult", StringComparison.Ordinal)
                && string.Equals(
                    x["source"]?.GetValue<string>(),
                    "custom",
                    StringComparison.Ordinal
                )
            ).ShouldBeTrue();

            var sortsExtension = GetPaginationExtension(sortsParameter);
            sortsExtension["fields"]?.AsArray().Any(x =>
                string.Equals(
                    x?["name"]?.GetValue<string>(),
                    "by_name_length",
                    StringComparison.Ordinal
                )
                && string.Equals(
                    x["source"]?.GetValue<string>(),
                    "custom",
                    StringComparison.Ordinal
                )
            ).ShouldBeTrue();
        }

        [Test]
        public async Task MicrosoftOpenApi_ShouldMatchSwagger_ForInferredFiltersAndSorts()
        {
            var path = "/openapi-pagination-tests/inferred";
            var swaggerOperation = GetSwaggerOperation(path);
            var microsoftOperation = await GetMicrosoftOpenApiOperationAsync(path);

            AssertQueryParameterDescriptionMatches(swaggerOperation, microsoftOperation, "filters");
            AssertQueryParameterDescriptionMatches(swaggerOperation, microsoftOperation, "sorts");
        }

        [Test]
        public async Task MicrosoftOpenApi_ShouldMatchSwagger_ForExplicitModelType()
        {
            var path = "/openapi-pagination-tests/explicit";
            var swaggerOperation = GetSwaggerOperation(path);
            var microsoftOperation = await GetMicrosoftOpenApiOperationAsync(path);

            AssertQueryParameterDescriptionMatches(swaggerOperation, microsoftOperation, "filters");
            AssertQueryParameterDescriptionMatches(swaggerOperation, microsoftOperation, "sorts");
        }

        [Test]
        public async Task MicrosoftOpenApi_ShouldMatchSwagger_ForNestedFields()
        {
            var path = "/openapi-pagination-tests/nested";
            var swaggerOperation = GetSwaggerOperation(path);
            var microsoftOperation = await GetMicrosoftOpenApiOperationAsync(path);

            AssertQueryParameterDescriptionMatches(swaggerOperation, microsoftOperation, "filters");
            AssertQueryParameterDescriptionMatches(swaggerOperation, microsoftOperation, "sorts");
        }

        [Test]
        public async Task MicrosoftOpenApi_ShouldMatchSwagger_ForCustomFilterAndSortMethods()
        {
            var path = "/openapi-pagination-tests/inferred";
            var swaggerOperation = GetSwaggerOperation(path, includeCustomMethods: true);
            var microsoftOperation = await GetMicrosoftOpenApiOperationAsync(path, includeCustomMethods: true);

            AssertQueryParameterDescriptionMatches(swaggerOperation, microsoftOperation, "filters");
            AssertQueryParameterDescriptionMatches(swaggerOperation, microsoftOperation, "sorts");
        }

        private static OpenApiOperation GetSwaggerOperation(string path, bool includeCustomMethods = false)
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

            RegisterCustomMethods(services, includeCustomMethods);
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

        private static async Task<OpenApiOperation> GetMicrosoftOpenApiOperationAsync(string path, bool includeCustomMethods = false)
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

            RegisterCustomMethods(services, includeCustomMethods);
            services.AddEndpointsApiExplorer();
            services.AddOpenApi("v1", options =>
            {
                options.AddPaginationOpenApiSupport();
            });

            using var serviceProvider = services.BuildServiceProvider();
            var openApiDocumentProvider = serviceProvider.GetRequiredKeyedService<IOpenApiDocumentProvider>("v1");
            var document = await openApiDocumentProvider.GetOpenApiDocumentAsync(default);
            return document.Paths[path].Operations![HttpMethod.Get];
        }

        private static void RegisterCustomMethods(IServiceCollection services, bool includeCustomMethods)
        {
            if (!includeCustomMethods)
            {
                return;
            }

            services.AddSingleton<PaginationOpenApiCustomMethods>();
            services.AddSingleton<IPaginationCustomFilterMethods>(x => x.GetRequiredService<PaginationOpenApiCustomMethods>());
            services.AddSingleton<IPaginationCustomSortsMethods>(x => x.GetRequiredService<PaginationOpenApiCustomMethods>());
        }

        private static void AssertQueryParameterDescriptionMatches(OpenApiOperation expected, OpenApiOperation actual, string parameterName)
        {
            var expectedParameter = GetQueryParameter(expected, parameterName);
            var actualParameter = GetQueryParameter(actual, parameterName);

            actualParameter.Description.ShouldBe(expectedParameter.Description);
            GetPrimaryExampleValue(actualParameter).ShouldBe(GetPrimaryExampleValue(expectedParameter));
            GetExampleValues(actualParameter).ShouldBe(GetExampleValues(expectedParameter));
            GetPaginationExtension(actualParameter).ToJsonString().ShouldBe(
                GetPaginationExtension(expectedParameter).ToJsonString()
            );
        }

        private static IOpenApiParameter GetQueryParameter(OpenApiOperation operation, string name)
        {
            return operation.Parameters!.Single(x =>
                x.In == ParameterLocation.Query &&
                string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
        }

        private static JsonObject GetPaginationExtension(IOpenApiParameter parameter)
        {
            parameter.Extensions.ShouldContainKey(PaginationOpenApiDescriptionBuilder.ExtensionName);
            parameter.Extensions[PaginationOpenApiDescriptionBuilder.ExtensionName]
                .ShouldBeOfType<JsonNodeExtension>();

            return ((JsonNodeExtension)parameter.Extensions[PaginationOpenApiDescriptionBuilder.ExtensionName]).Node
                .ShouldBeOfType<JsonObject>();
        }

        private static string? GetPrimaryExampleValue(IOpenApiParameter parameter)
        {
            return GetJsonNodeString(parameter.Example);
        }

        private static Dictionary<string, string?> GetExampleValues(IOpenApiParameter parameter)
        {
            if (parameter.Examples == null)
            {
                return new Dictionary<string, string?>();
            }

            return parameter.Examples.ToDictionary(
                x => x.Key,
                x => GetJsonNodeString(x.Value.Value)
            );
        }

        private static string? GetJsonNodeString(JsonNode? node)
        {
            if (node == null)
            {
                return null;
            }

            if (node is JsonValue value)
            {
                return value.GetValue<string>();
            }

            return node.ToJsonString();
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
