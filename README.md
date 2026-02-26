# tools-dotnet

`tools-dotnet` is a shared .NET library for common backend patterns in `hodl-software` projects.

It gives you:

- Generic CRUD repository and service base classes.
- Paging, filtering, and sorting for EF Core queries.
- OpenAPI metadata generation for filter and sort query parameters.
- API error models and exception helpers.
- Utility helpers for strings, urls, parsing, streaming, and async queues.

## Package overview

- `tools_dotnet.Dao`
Contains generic repository contracts and base implementations (with and without DTO mapping and key wrappers).

- `tools_dotnet.Service`
Contains generic service contracts and base implementations built on top of the DAO layer.

- `tools_dotnet.Paging`
Contains request and response models for paging (`IApiPagination`, `IPagedList`, `PagingMetadata`).

- `tools_dotnet.Pagination`
Contains parsing and query expression logic for filter/sort/page, plus OpenAPI support.

- `tools_dotnet.Errors` and `tools_dotnet.Exceptions`
Contains reusable API error payloads and domain exceptions.

- `tools_dotnet.Utility`
Contains focused helper extensions and helper classes.

## Pagination, filtering, and sorting

The pagination engine is compatible with Sieve-style query syntax and works on `IQueryable` (including EF Core queries).

### Main types

- `tools_dotnet.Paging.IApiPagination`
- `tools_dotnet.Paging.Impl.ApiPagination`
- `tools_dotnet.Pagination.Services.IPaginationProcessor`
- `tools_dotnet.Pagination.Services.PaginationProcessor`
- `tools_dotnet.Pagination.Attributes.PaginationAttribute`
- `tools_dotnet.Utility.QueryableExtensions`

### Basic setup

Mark filter/sort fields on your entity (or DTO used for mapping):

```csharp
using tools_dotnet.Pagination.Attributes;

public sealed class UserEntity
{
    [Pagination(Name = "name", CanFilter = true, CanSort = true)]
    public string Name { get; set; } = string.Empty;

    [Pagination(Name = "age", CanFilter = true, CanSort = true)]
    public int Age { get; set; }

    [Pagination(Name = "created_at", CanFilter = true, CanSort = true)]
    public DateTimeOffset CreatedAt { get; set; }
}
```

Nested objects can be exposed explicitly:

```csharp
public sealed class UserEntity
{
    [Pagination(
        Name = "profile",
        CanFilter = false,
        CanSort = false,
        CanFilterSubProperties = true,
        CanSortSubProperties = true)]
    public UserProfile Profile { get; set; } = new();
}

public sealed class UserProfile
{
    [Pagination(Name = "display_name", CanFilter = true, CanSort = true)]
    public string DisplayName { get; set; } = string.Empty;
}
```

Example queries:

- `filters=profile.display_name==alice`
- `sorts=profile.display_name`

Accept query parameters in your endpoint:

```csharp
using Microsoft.AspNetCore.Mvc;
using tools_dotnet.Paging.Impl;

[HttpGet]
public async Task<IActionResult> GetUsers([FromQuery] ApiPagination pagination)
{
    ...
}
```

Apply filtering, sorting, and paging:

```csharp
using tools_dotnet.Pagination.Services;
using tools_dotnet.Utility;

public async Task<IPagedList<UserEntity>> GetAllAsync(IApiPagination pagination)
{
    var processor = new PaginationProcessor();

    return await _dbContext.Set<UserEntity>()
        .AsQueryable()
        .SortFilterAndPageAsync(pagination, processor);
}
```

### Filter and sort syntax

Filters format: `field{operator}value`

- `,` for AND between filter terms.
- `|` for OR between values.
- `(fieldA|fieldB)` for OR between fields.
- `\` to escape special characters.
- `null` means null value.
- `\null` means the literal string `null`.

Sort format: `sorts=field,-otherField`

- `field` ascending
- `-field` descending
- `,` for multi-sort

Supported operators:

- Equality: `==`, `!=`
- Case-insensitive equality: `==*`, `!=*`
- Comparison: `>`, `>=`, `<`, `<=`
- Contains: `@=`, `!@=`, `@=*`, `!@=*`
- Starts with: `_=`, `!_=`, `_=*`, `!_=*`
- Ends with: `_-=`, `!_-=`, `_-=*`, `!_-=*`

### SQL Server vs PostgreSQL case-insensitive behavior

Case-insensitive string operators can normalize values using:

- `PaginationCaseInsensitiveNormalization.ToUpper`
- `PaginationCaseInsensitiveNormalization.ToLower`
- `PaginationCaseInsensitiveNormalization.None`

Built-in presets:

```csharp
using tools_dotnet.Pagination.Services;

// SQL Server style
var sqlServerProcessor = new PaginationProcessor(
    filterExpressionProviders: new[] { new SqlServerPaginationFilterExpressionProvider() });

// PostgreSQL style
var postgreSqlProcessor = new PaginationProcessor(
    filterExpressionProviders: new[] { new PostgreSqlPaginationFilterExpressionProvider() });

// PostgreSQL citext style (rely on db type/collation)
var citextProcessor = new PaginationProcessor(
    filterExpressionProviders: new[]
    {
        new PostgreSqlPaginationFilterExpressionProvider(PaginationCaseInsensitiveNormalization.None)
    });
```

## OpenAPI support for filters and sorts

Pagination OpenAPI support can enrich `filters` and `sorts` query parameter descriptions in `openapi.json` by reading `[Pagination]` attributes.
Nested fields are included when parent members allow sub-properties (`CanFilterSubProperties` / `CanSortSubProperties`).

Supported integrations:

- Swashbuckle (`AddSwaggerGen`)
- ASP.NET Core OpenAPI (`AddOpenApi`)

Custom method containers (`IPaginationCustomFilterMethods` / `IPaginationCustomSortsMethods`) are included in docs when registered in DI, or passed to `AddPaginationOpenApiSupport(...)`.

### Swashbuckle / SwaggerGen

```csharp
using Microsoft.OpenApi.Models;
using tools_dotnet.Pagination.OpenApi;

services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "My API", Version = "v1" });
    options.AddPaginationOpenApiSupport();

    // Optional explicit custom method mapping:
    // options.AddPaginationOpenApiSupport(
    //     customFilterMethods: new[] { new UserCustomFilters() },
    //     customSortMethods: new[] { new UserCustomSorts() });
});
```

### Microsoft.AspNetCore.OpenApi

```csharp
using tools_dotnet.Pagination.OpenApi;

services.AddOpenApi("v1", options =>
{
    options.AddPaginationOpenApiSupport();

    // Optional explicit custom method mapping:
    // options.AddPaginationOpenApiSupport(
    //     customFilterMethods: new[] { new UserCustomFilters() },
    //     customSortMethods: new[] { new UserCustomSorts() });
});
```

When your endpoint does not return `IPagedList<T>`, set the model explicitly:

```csharp
using tools_dotnet.Pagination.OpenApi;

[PaginationOpenApiType(typeof(UserEntity))]
[HttpGet("users")]
public ActionResult<IReadOnlyList<UserEntity>> GetUsers([FromQuery] ApiPagination pagination)
{
    ...
}
```

## CRUD repository and service base classes

### DAO layer

The DAO layer gives you reusable generic repository patterns:

- `ICrudRepo<TEntity, TIdType>` and `BaseCrudRepo<TEntity, TIdType>`
- `ICrudDtoRepo<TEntity, TIdType, TDto>` and `BaseCrudDtoRepo<TEntity, TIdType, TDto>`
- `ICrudRepoWithKeyWrapper<TEntity, TKeyWrapper>`
- `ICrudDtoRepoWithKeyWrapper<TEntity, TKeyWrapper, TDto>`
- `ISortFilterAndPageRepo<TEntity>` / `ISortFilterAndPageDtoRepo<TEntity, TDto>`

Current base implementations include:

- Automatic soft-delete filtering for `IAuditableEntity`.
- Paging/filter/sort integration through `IPaginationProcessor`.
- PostgreSQL-specific exception mapping for FK and unique violations.

### Service layer

The service layer wraps repository access and validation:

- `ICrudService<TDto, TIdType>` and `BaseCrudService<...>`
- `ICrudServiceWithKeyWrapper<TEntity, TKeyWrapper, TDto>` and `BaseCrudServiceWithKeyWrapper<...>`

Base services call FluentValidation before `Add`/`Update`.

### Entity helpers

- `IEntity` and `IEntityWithId<T>` for base entity contracts.
- `IChangeTrackingEntity` and `IAuditableEntity` for created/updated/deleted timestamps.
- `TimestampsInterceptor` to auto-populate created/updated timestamps in EF Core `SaveChanges`.
- `IKeyWrapper<TEntity>` for nested-resource key handling and scoped filtering.

## Error and exception helpers

- `tools_dotnet.Errors.GenericApiError` and specific error payloads (`ApiValidationError`, `ApiItemNotFoundError`, etc.).
- `tools_dotnet.Exceptions.*` for domain exceptions.
- `tools_dotnet.Utility.GenericErrorExtensions.MapExceptionToApiError(...)` to map known exceptions to API errors.

## Utilities (all classes in `tools_dotnet.Utility`)

- `AsyncQueue<T>`
FIFO async queue wrapper for producer/consumer workflows with safe single-enumerator behavior.

- `EmailStringExtensions`
Parses one or many email strings and extracts normalized email addresses.

- `GenericErrorExtensions`
Maps known `tools_dotnet.Exceptions` and FluentValidation exceptions to API error models.

- `ParseExtensions`
Parses collections of strings into typed values (`IParsable<T>`) with configurable failure behavior.

- `QueryableExtensions`
Applies pagination/filter/sort to `IQueryable` and supports projection to DTOs with AutoMapper.

- `ResultStreamExtensions`
Streams `IAsyncEnumerable<T>` responses as NDJSON (`application/x-ndjson`) with immediate flushing.

- `SnakeCaseEnumConverter<T>`
`System.Text.Json` enum converter that serializes enum names in snake_case.

- `StringExtensions`
String helpers including snake_case conversion, join-with-non-empty semantics, and Jaro-Winkler proximity scoring.

- `UrlStringExtensions`
URL helpers for sanitize/normalize behavior, relative path resolution, domain extraction, and query removal.

## Custom filter expression providers

You can plug in your own `IPaginationFilterExpressionProvider`:

```csharp
using System.Linq.Expressions;
using tools_dotnet.Pagination.Services;

public sealed class MyCustomProvider : IPaginationFilterExpressionProvider
{
    public bool TryBuildExpression(PaginationFilterExpressionContext context, out Expression? expression)
    {
        expression = null;
        return false;
    }
}
```

If you add custom providers and still want built-in behavior, include `DefaultPaginationFilterExpressionProvider` in your provider list.

## Custom filter methods (Sieve-style)

When a filter field does not map to an entity member, `PaginationProcessor` can call custom methods by name.

Implement `IPaginationCustomFilterMethods` and register the implementation in the processor:

```csharp
using tools_dotnet.Pagination.Services;

public sealed class UserCustomFilters : IPaginationCustomFilterMethods
{
    public IQueryable<UserEntity> is_adult(IQueryable<UserEntity> source, string op, string[] values)
    {
        if (op != "==" || values.Length == 0 || !int.TryParse(values[0], out var minAge))
        {
            return source;
        }

        return source.Where(x => x.Age >= minAge);
    }
}

var processor = new PaginationProcessor(
    customFilterMethods: new[] { new UserCustomFilters() });
```

Query example:

- `filters=is_adult==21`

Supported filter signatures:

- `IQueryable<TEntity> Method(IQueryable<TEntity> source)`
- `IQueryable<TEntity> Method(IQueryable<TEntity> source, string op)`
- `IQueryable<TEntity> Method(IQueryable<TEntity> source, string op, string[] values)`
- Same with an extra fourth `object[]?` argument for custom data.

## Custom sort methods (Sieve-style)

When a sort field does not map to an entity member, `PaginationProcessor` can call custom sort methods by name.

Implement `IPaginationCustomSortsMethods` and register the implementation in the processor:

```csharp
using tools_dotnet.Pagination.Services;

public sealed class UserCustomSorts : IPaginationCustomSortsMethods
{
    public IQueryable<UserEntity> popularity(IQueryable<UserEntity> source, bool useThenBy, bool desc)
    {
        if (useThenBy)
        {
            var ordered = (IOrderedQueryable<UserEntity>)source;
            return desc
                ? ordered.ThenByDescending(x => x.LikeCount)
                : ordered.ThenBy(x => x.LikeCount);
        }

        return desc
            ? source.OrderByDescending(x => x.LikeCount)
            : source.OrderBy(x => x.LikeCount);
    }
}

var processor = new PaginationProcessor(
    customSortMethods: new[] { new UserCustomSorts() });
```

Query example:

- `sorts=popularity,-created_at`

Supported sort signatures:

- `IQueryable<TEntity> Method(IQueryable<TEntity> source)`
- `IQueryable<TEntity> Method(IQueryable<TEntity> source, bool useThenBy)`
- `IQueryable<TEntity> Method(IQueryable<TEntity> source, bool useThenBy, bool desc)`
- Same with an extra fourth `object[]?` argument for custom data.
