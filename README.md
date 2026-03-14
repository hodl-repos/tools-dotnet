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

- `tools_dotnet.Enum`
Contains shared enums such as `SoftDeleteQueryMode` and `StringCaseType`.

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

Pagination OpenAPI support can enrich `filters` and `sorts` in `openapi.json` by reading `[Pagination]` attributes.
The generated docs now include:

- readable parameter descriptions with shorter syntax guidance
- concrete query examples for `filters` and `sorts`
- machine-readable `x-tools-dotnet-pagination` metadata for tooling/codegen

Nested fields are included when parent members allow sub-properties (`CanFilterSubProperties` / `CanSortSubProperties`).

Supported integrations:

- Swashbuckle (`AddSwaggerGen`)
- ASP.NET Core OpenAPI (`AddOpenApi`)

Custom method containers (`IPaginationCustomFilterMethods` / `IPaginationCustomSortsMethods`) are included in docs when registered in DI, or passed to `AddPaginationOpenApiSupport(...)`.

Example output for the `filters` parameter:

```json
{
  "name": "filters",
  "in": "query",
  "description": "Syntax: field{operator}value. Use ',' for AND and '|' for OR.",
  "example": "name==sample",
  "examples": {
    "simple": { "summary": "Simple equality filter", "value": "name==sample" },
    "comparison": { "summary": "Comparison filter", "value": "age>=42" },
    "combined": { "summary": "Combined AND filter", "value": "name==sample,enabled==true" }
  },
  "x-tools-dotnet-pagination": {
    "mode": "filters",
    "syntax": {
      "expression": "field{operator}value",
      "andSeparator": ",",
      "orSeparator": "|",
      "escapeCharacter": "\\",
      "nullLiteral": "null"
    },
    "examples": [
      "name==sample",
      "age>=42",
      "name==sample,enabled==true"
    ],
    "fields": [
      {
        "name": "name",
        "type": "string",
        "operators": ["==", "==*", "!=", "!=*", "@=", "@=*", "!@=", "!@=*", "_=", "_=*", "!_=", "!_=*", "_-=", "_-=*", "!_-=", "!_-=*"],
        "source": "member"
      }
    ]
  }
}
```

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
- Provider-neutral exception mapping for common FK/dependency and unique-key violations.

### Soft-delete lifecycle

For `IAuditableEntity` types, the default read APIs now stay on the active view:

- `GetAllAsync(...)` and `GetByIdAsync(...)` exclude soft-deleted rows.
- `RemoveAsync(...)` performs a soft delete.

Use `SoftDeleteQueryMode` on repo read APIs when you need administrative access to deleted rows:

- `GetAllAsync(SoftDeleteQueryMode.IncludeDeleted, ...)`
- `GetAllAsync(SoftDeleteQueryMode.DeletedOnly, ...)`
- `GetByIdAsync(id, SoftDeleteQueryMode.IncludeDeleted, ...)`
- `GetAllDtoAsync(SoftDeleteQueryMode.IncludeDeleted, ...)`
- `GetAllDtoAsync(SoftDeleteQueryMode.DeletedOnly, ...)`
- `GetByIdDtoAsync(id, SoftDeleteQueryMode.IncludeDeleted, ...)`

The restore and hard-delete lifecycle stays explicit:

- `RestoreAsync(...)`
- `HardRemoveAsync(...)`

The service layer still exposes convenience methods like `GetAllIncludingDeletedAsync(...)` and
`GetAllDeletedAsync(...)`, but those now delegate to the repo-level `SoftDeleteQueryMode` APIs.

Concurrency-aware repos and services also expose token-aware `RestoreAsync(...)` and `HardRemoveAsync(...)` overloads.

### Service layer

The service layer wraps repository access and validation:

- `ICrudService<TDto, TIdType>` and `BaseCrudService<...>`
- `ICrudServiceWithKeyWrapper<TEntity, TKeyWrapper, TDto>` and `BaseCrudServiceWithKeyWrapper<...>`

Base services call FluentValidation before `Add`/`Update`.

### Entity helpers

- `IEntity` and `IEntityWithId<T>` for base entity contracts.
- `IChangeTrackingEntity` and `IAuditableEntity` for created/updated/deleted timestamps.
- `IChangeTrackingDto` for DTOs that round-trip `CreatedTimestamp` / `UpdatedTimestamp`.
- `TimestampsInterceptor` to auto-populate created/updated timestamps in EF Core `SaveChanges`.
- `IKeyWrapper<TEntity>` for nested-resource key handling and scoped filtering.

`CreatedTimestamp` is treated as immutable after insert.
On update, incoming `CreatedTimestamp` values are ignored and the original stored value is preserved; only `UpdatedTimestamp` is advanced.

### Optimistic concurrency

The concurrency-aware CRUD variants add optimistic concurrency using a configurable token:

- `IConcurrentCrudRepo<...>` / `BaseConcurrentCrudRepo<...>`
- `IConcurrentCrudDtoRepo<...>` / `BaseConcurrentCrudDtoRepo<...>`
- `IConcurrentCrudRepoWithKeyWrapper<...>` / `BaseConcurrentCrudRepoWithKeyWrapper<...>`
- `IConcurrentCrudDtoRepoWithKeyWrapper<...>` / `BaseConcurrentCrudDtoRepoWithKeyWrapper<...>`
- `IConcurrentCrudService<...>` / `BaseConcurrentCrudService<...>`
- `IConcurrentCrudServiceWithKeyWrapper<...>` / `BaseConcurrentCrudServiceWithKeyWrapper<...>`

The legacy `BaseCrud...` and `BaseCrudService...` types stay non-concurrent and preserve the old behavior.

By default, the concurrent variants use `UpdatedTimestamp`, and `UpdateAsync(item)` is enforced when the request payload exposes the configured token property.

Example DTO:

```csharp
using tools_dotnet.Dto;

public sealed class UserDto : IDtoWithId<int>, IChangeTrackingDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTimeOffset CreatedTimestamp { get; set; }
    public DateTimeOffset? UpdatedTimestamp { get; set; }
}
```

Update requests should round-trip the `UpdatedTimestamp` from the last read.
If the stored value no longer matches, the repo throws `ConcurrentModificationException`.

You can also fetch the current token before an update or delete:

```csharp
var token = await userRepo.GetConcurrencyTokenAsync(userId);
await userRepo.UpdateAsync(userDto, token);
await userRepo.RemoveAsync(userId, token);
```

You can opt into other token styles when constructing a concurrent base repo:

```csharp
using tools_dotnet.Dao.Crud;

public sealed class UserRepo : BaseConcurrentCrudDtoRepo<User, int, UserDto, byte[]>
{
    public UserRepo(DbContext dbContext, IMapper mapper, IPaginationProcessor paginationProcessor)
        : base(
            dbContext,
            mapper,
            paginationProcessor,
            CrudConcurrencyConfiguration.SqlServerRowVersion("RowVersion")
        ) { }
}
```

Built-in helpers:

- `CrudConcurrencyConfiguration.UpdatedTimestamp()` for timestamp-based concurrency.
- `CrudConcurrencyConfiguration.SqlServerRowVersion()` for SQL Server `rowversion` / `timestamp` style properties.
- `CrudConcurrencyConfiguration.PostgreSqlXmin()` for PostgreSQL `xmin` properties mapped into your entity.
- `CrudConcurrencyConfiguration.ForProperty<TConcurrencyToken>(...)` for custom token properties such as `Guid`, `string`, `byte[]`, or renamed DTO fields.

If your database generates the token during `UPDATE` (for example, a trigger-generated UUID), use the explicit overloads even when the input DTO does not carry the token:

```csharp
var token = await userRepo.GetConcurrencyTokenAsync(userId);

await userRepo.UpdateAsync(updateDtoWithoutToken, token);
await userRepo.RemoveAsync(userId, token);
```

The legacy non-concurrent repos and services still expose the old `RemoveAsync(...)` overloads. The concurrent variants require the token-aware overloads for deletes.

## Error and exception helpers

- `tools_dotnet.Errors.GenericApiError` and specific error payloads (`ApiValidationError`, `ApiItemNotFoundError`, etc.).
- `tools_dotnet.Exceptions.*` for domain exceptions.
- `tools_dotnet.Utility.GenericErrorExtensions.MapExceptionToApiError(...)` to map known exceptions to API errors.

The CRUD base repos also translate common database constraint failures into domain exceptions:

- `ConflictingItemException` for duplicate/unique constraint violations.
- `DependentItemException` for delete/update operations blocked by dependent rows.

This translation is provider-neutral in the library surface and is currently verified against SQL Server and PostgreSQL container-backed integration tests.

## Utilities (all classes in `tools_dotnet.Utility`)

- `AsyncQueue<T>`
FIFO async queue wrapper for producer/consumer workflows with safe single-enumerator behavior.

- `EmailStringExtensions`
Parses one or many email strings and extracts normalized email addresses.

- `EnumCaseConverter`
`System.Text.Json` enum converter that formats enum names using `StringCaseType` and `StringCaseExtensions`.

- `GenericErrorExtensions`
Maps known `tools_dotnet.Exceptions` and FluentValidation exceptions to API error models.

- `GZipCompressionHelper`
Compresses and decompresses `byte[]` and `string` payloads, including base64 helpers for transport/storage scenarios.

- `ParseExtensions`
Parses collections of strings into typed values (`IParsable<T>`) with configurable failure behavior.

- `QueryableExtensions`
Applies pagination/filter/sort to `IQueryable` and supports projection to DTOs with AutoMapper.

- `ResultStreamExtensions`
Streams `IAsyncEnumerable<T>` responses as NDJSON (`application/x-ndjson`) with immediate flushing.

- `StringCaseExtensions`
Fast string case conversion helpers for `snake_case`, `kebab-case`, `dot.case`, `COBOL-CASE`, `SCREAMING_SNAKE_CASE`, `PascalCase`, and `camelCase`.

- `StringExtensions`
General string helpers such as join-with-non-empty semantics and Jaro-Winkler proximity scoring.

- `UrlStringExtensions`
URL helpers for sanitize/normalize behavior, relative path resolution, domain extraction, and query removal.

## String casing and enum JSON conversion

Use `StringCaseExtensions` when you need fast name conversion in application code:

```csharp
using tools_dotnet.Utility;

var snake = "XMLHttpRequest".ToSnakeCase();              // xml_http_request
var kebab = "HttpStatusOk".ToKebabCase();               // http-status-ok
var cobol = "HttpStatusOk".ToCobolCase();               // HTTP-STATUS-OK
var camel = "http_status_ok".ToCamelCase();             // httpStatusOk
var pascal = "user-name-value".ToPascalCase();          // UserNameValue
```

For enum JSON serialization, configure `EnumCaseConverter` with a `StringCaseType`:

```csharp
using System.Text.Json;
using tools_dotnet.Enum;
using tools_dotnet.Utility;

public enum SyncStatus
{
    AwaitingReview,
    HttpStatusOk,
}

var options = new JsonSerializerOptions();
options.Converters.Add(new EnumCaseConverter(StringCaseType.SnakeCase));

var json = JsonSerializer.Serialize(SyncStatus.HttpStatusOk, options);
// "http_status_ok"

var value = JsonSerializer.Deserialize<SyncStatus>("\"awaiting_review\"", options);
```

Typical ASP.NET Core registration:

```csharp
using tools_dotnet.Enum;
using tools_dotnet.Utility;

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(
        new EnumCaseConverter(StringCaseType.UpperKebabCase));
});
```

Available case styles:

- `StringCaseType.Original`
- `StringCaseType.CamelCase`
- `StringCaseType.PascalCase`
- `StringCaseType.SnakeCase`
- `StringCaseType.KebabCase`
- `StringCaseType.UpperKebabCase` / `StringCaseType.CobolCase`
- `StringCaseType.ScreamingSnakeCase` / `StringCaseType.UpperSnakeCase`
- `StringCaseType.DotCase`

## Obsolete and removed APIs

Recent changes replaced a few older APIs. If you are upgrading, use these mappings:

- `SnakeCaseEnumConverter<T>` was removed. Replace it with `new EnumCaseConverter(StringCaseType.SnakeCase)`.
- `UpperKebabCaseEnumConverter<TEnum>` was removed. Replace it with `new EnumCaseConverter(StringCaseType.UpperKebabCase)` or `StringCaseType.CobolCase`.
- Repo `GetAllIncludingDeletedAsync(...)` was removed. Replace it with `GetAllAsync(SoftDeleteQueryMode.IncludeDeleted, ...)`.
- Repo `GetAllDeletedAsync(...)` was removed. Replace it with `GetAllAsync(SoftDeleteQueryMode.DeletedOnly, ...)`.
- Repo `GetByIdIncludingDeletedAsync(...)` was removed. Replace it with `GetByIdAsync(id, SoftDeleteQueryMode.IncludeDeleted, ...)`.
- DTO repo `GetAllDtoIncludingDeletedAsync(...)` was removed. Replace it with `GetAllDtoAsync(SoftDeleteQueryMode.IncludeDeleted, ...)`.
- DTO repo `GetAllDeletedDtoAsync(...)` was removed. Replace it with `GetAllDtoAsync(SoftDeleteQueryMode.DeletedOnly, ...)`.
- DTO repo `GetByIdDtoIncludingDeletedAsync(...)` was removed. Replace it with `GetByIdDtoAsync(id, SoftDeleteQueryMode.IncludeDeleted, ...)`.
- `FindAsync(..., ignoreDeletedWithAuditable: false, ...)` was removed. Replace it with `FindAsync(..., SoftDeleteQueryMode.IncludeDeleted, ...)`.
- `FindDtoAsync(..., ignoreDeletedWithAuditable: false, ...)` was removed. Replace it with `FindDtoAsync(..., SoftDeleteQueryMode.IncludeDeleted, ...)`.

Before:

```csharp
var deletedUsers = await userRepo.GetAllIncludingDeletedAsync(cancellationToken);
var deletedDtos = await userDtoRepo.GetAllDeletedDtoAsync(cancellationToken);
var user = await userRepo.GetByIdIncludingDeletedAsync(id, cancellationToken);
```

After:

```csharp
var deletedUsers = await userRepo.GetAllAsync(
    SoftDeleteQueryMode.IncludeDeleted,
    cancellationToken);

var deletedDtos = await userDtoRepo.GetAllDtoAsync(
    SoftDeleteQueryMode.DeletedOnly,
    cancellationToken);

var user = await userRepo.GetByIdAsync(
    id,
    SoftDeleteQueryMode.IncludeDeleted,
    cancellationToken);
```

Note:
Service-level convenience methods such as `GetAllIncludingDeletedAsync(...)`,
`GetAllDeletedAsync(...)`, and `GetByIdIncludingDeletedAsync(...)` still exist.
The soft-delete consolidation only changed the repo-level APIs.

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
