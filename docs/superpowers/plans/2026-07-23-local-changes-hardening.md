# Local Changes Hardening Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Harden the local clock and pagination changes, restore effective default sorting, improve focused tests and error behavior, and update every central NuGet version to its latest stable release.

**Architecture:** Keep the existing public extension points and wire format. Add regression coverage at each public boundary, make pagination failure classification explicit, and apply dependency upgrades centrally through `Directory.Packages.props`.

**Tech Stack:** .NET 10, C#, Entity Framework Core, ASP.NET Core OpenAPI, Swashbuckle, NUnit, Shouldly, Testcontainers, NuGet central package management.

## Global Constraints

- Preserve existing public APIs unless a newly added local API is defective or unsafe.
- Keep every existing direct NuGet package reference.
- Use latest stable versions only, including major releases.
- Preserve custom pagination filter and sort fallbacks.
- Explicit client sorting overrides attribute-based default sorting.
- Add a failing regression test before each production defect fix.
- Avoid unrelated repository-wide refactoring.

---

### Task 1: Restore And Verify Default Sorting

**Files:**
- Modify: `tools-dotnet/Pagination/Attributes/PaginationAttribute.cs`
- Modify: `tools-dotnet/Pagination/Services/PaginationProcessor.cs`
- Modify: `tools-dotnet/Pagination/OpenApi/PaginationOpenApiFieldDescriptor.cs`
- Modify: `tools-dotnet/Pagination/OpenApi/PaginationOpenApiMetadataProvider.cs`
- Modify: `tools-dotnet/Pagination/OpenApi/PaginationOpenApiDescriptionBuilder.cs`
- Modify: `tools-dotnet.Tests/PaginationTest/PaginationProcessorTests.cs`
- Modify: `tools-dotnet.Tests/PaginationTest/PaginationOpenApiOperationFilterTests.cs`
- Modify: `README.md`

**Interfaces:**
- Produces: `PaginationAttribute.IsDefaultSorted`
- Produces: `PaginationAttribute.DefaultSortDescending`
- Produces: default `PaginationSortTerm` values when explicit sorts are empty
- Produces: OpenAPI fields `isDefaultSorted` and `defaultSortDirection`

- [ ] **Step 1: Make the existing OpenAPI assertion effective**

Replace null-conditional assertions with required-node assertions:

```csharp
defaultSortedFilterField["isDefaultSorted"]
    .ShouldNotBeNull()
    .GetValue<bool>()
    .ShouldBeTrue();
defaultSortedFilterField["defaultSortDirection"]
    .ShouldNotBeNull()
    .GetValue<string>()
    .ShouldBe("desc");
```

- [ ] **Step 2: Run the focused OpenAPI test and verify RED**

Run:

```bash
dotnet test tools-dotnet.Tests/tools-dotnet.Tests.csproj --no-restore --filter "FullyQualifiedName~Swagger_ShouldDocumentFiltersAndSorts_FromReturnType"
```

Expected: FAIL because the default-sort metadata is absent.

- [ ] **Step 3: Add processor behavior tests and verify RED**

Add tests for ascending, descending, multiple defaults, nested defaults, blocked
defaults, explicit-sort precedence, and `applySorting: false`.

Representative assertion:

```csharp
var result = processor
    .Apply(new PaginationModel(), source, applyFiltering: false, applyPagination: false)
    .Select(x => x.Id)
    .ToList();

result.ShouldBe([2, 3, 1]);
```

Run:

```bash
dotnet test tools-dotnet.Tests/tools-dotnet.Tests.csproj --no-restore --filter "FullyQualifiedName~PaginationProcessorTests"
```

Expected: the new default-sort tests FAIL because the attribute and processor
behavior are absent.

- [ ] **Step 4: Implement default sorting**

Add the two attribute properties, cache default sort terms by entity type, walk
only sortable member paths, and apply those terms only when the deserialized
sort list is empty. Keep explicit sort order untouched.

Core selection:

```csharp
var sorts = model.Sorts.Count == 0
    ? GetDefaultSorts(typeof(TEntity))
    : model.Sorts;
query = ApplySorts(query, sorts, dataForCustomMethods);
```

- [ ] **Step 5: Add OpenAPI metadata**

Carry default-sort state through `PaginationOpenApiFieldDescriptor` and emit:

```csharp
if (field.IsDefaultSorted)
{
    fieldObject["isDefaultSorted"] = true;
    fieldObject["defaultSortDirection"] =
        field.DefaultSortDescending ? "desc" : "asc";
}
```

Only members that are also sortable may be described as default sorted.

- [ ] **Step 6: Verify GREEN**

Run:

```bash
dotnet test tools-dotnet.Tests/tools-dotnet.Tests.csproj --no-restore --filter "FullyQualifiedName~PaginationProcessorTests|FullyQualifiedName~PaginationOpenApiOperationFilterTests"
```

Expected: all selected tests PASS.

- [ ] **Step 7: Update README and commit**

Document ascending and descending defaults and explicit-sort precedence.

```bash
git add README.md tools-dotnet/Pagination tools-dotnet.Tests/PaginationTest/PaginationProcessorTests.cs tools-dotnet.Tests/PaginationTest/PaginationOpenApiOperationFilterTests.cs
git commit -m "feat: restore pagination default sorting"
```

### Task 2: Harden Clock Provider Coverage

**Files:**
- Modify: `tools-dotnet.Tests/CrudTest/ClockProviderTests.cs`
- Modify only if a regression test fails: `tools-dotnet/Dao/Interceptors/TimestampsInterceptor.cs`
- Modify only if a regression test fails: `tools-dotnet/Dao/Crud/Impl/BaseCrudRepo.cs`
- Modify only if a regression test fails: `tools-dotnet/Dao/Crud/Impl/BaseConcurrentCrudRepo.cs`
- Modify only if a regression test fails: `tools-dotnet/Dao/Crud/Impl/BaseCrudRepoWithKeyWrapper.cs`
- Modify only if a regression test fails: `tools-dotnet/Dao/Crud/Impl/BaseConcurrentCrudRepoWithKeyWrapper.cs`

**Interfaces:**
- Consumes: `IClockProvider.UtcNow`
- Preserves: optional clock constructor parameters and `SystemClockProvider.Instance`

- [ ] **Step 1: Add missing behavioral coverage**

Add tests for synchronous `SaveChanges`, modified-entity timestamps, preservation
of `CreatedTimestamp`, cancellation-token repo overloads, and default provider
construction.

Representative synchronous test:

```csharp
dbContext.ClockEntities.Add(entity);
dbContext.SaveChanges();

entity.CreatedTimestamp.ShouldBe(now);
entity.UpdatedTimestamp.ShouldBe(now);
```

- [ ] **Step 2: Run focused clock tests**

Run:

```bash
dotnet test tools-dotnet.Tests/tools-dotnet.Tests.csproj --no-restore --filter "FullyQualifiedName~ClockProviderTests"
```

Expected: characterization tests PASS; any failing behavior becomes a regression
cycle before production changes.

- [ ] **Step 3: Refactor test duplication while green**

Use parameterized fixtures or shared helpers for the four repository variants.
Keep assertions on persisted entities rather than constructor or mock calls.

- [ ] **Step 4: Verify and commit**

Run the focused clock tests again and expect all to PASS.

```bash
git add tools-dotnet.Tests/CrudTest/ClockProviderTests.cs tools-dotnet/Dao
git commit -m "test: harden clock provider coverage"
```

### Task 3: Harden The Type-Safe Filter Builder

**Files:**
- Modify: `tools-dotnet/Pagination/Builders/PaginationFilterBuilder.cs`
- Modify: `tools-dotnet/Pagination/Builders/CreateFilter.cs`
- Modify: `tools-dotnet.Tests/PaginationTest/PaginationFilterBuilderTests.cs`
- Modify: `README.md`

**Interfaces:**
- Preserves: all `CreateFilter<TEntity>` and `PaginationFilterBuilder<TEntity>` overloads
- Produces: deterministic validation errors for invalid expressions and empty collections

- [ ] **Step 1: Add failing edge-case tests**

Add tests that reject member-to-member comparisons and empty field/value
collections with `ArgumentException`, while preserving constants and captured
values.

```csharp
Should.Throw<ArgumentException>(() =>
    CreateFilter<FilterBuilderEntity>.And(x => x.Age == x.OtherAge));

Should.Throw<ArgumentException>(() =>
    CreateFilter<FilterBuilderEntity>.AndValues(
        x => x.Age,
        Array.Empty<int>(),
        PaginationOperator.Equal));
```

- [ ] **Step 2: Verify RED**

Run:

```bash
dotnet test tools-dotnet.Tests/tools-dotnet.Tests.csproj --no-restore --filter "FullyQualifiedName~PaginationFilterBuilderTests"
```

Expected: the new invalid-input tests FAIL with an implementation exception or
an invalid empty filter term.

- [ ] **Step 3: Implement minimal validation**

Reject value expressions that still reference the entity parameter before
compilation, and require at least one field and value in `AddTerm`.

```csharp
if (ContainsParameter(expression))
{
    throw new ArgumentException(
        "Predicate values cannot reference the filter entity.",
        nameof(expression));
}
```

- [ ] **Step 4: Refactor formatting and validation**

Centralize enumerable materialization in `AddTerm`, keep invariant formatting,
and retain exact escaping symmetry with `PaginationModelDeserializer`.

- [ ] **Step 5: Verify and commit**

Run focused builder and deserializer tests; expect all to PASS.

```bash
git add README.md tools-dotnet/Pagination/Builders tools-dotnet.Tests/PaginationTest/PaginationFilterBuilderTests.cs
git commit -m "feat: harden pagination filter builder"
```

### Task 4: Make Pagination Errors Precise

**Files:**
- Modify: `tools-dotnet/Exceptions/PaginationErrorCode.cs`
- Modify: `tools-dotnet/Exceptions/InvalidPaginationFilterException.cs`
- Modify: `tools-dotnet/Exceptions/InvalidPaginationSortException.cs`
- Modify: `tools-dotnet/Pagination/Services/PaginationModelDeserializer.cs`
- Modify: `tools-dotnet/Pagination/Services/PaginationProcessor.cs`
- Modify: `tools-dotnet/Errors/ApiPaginationError.cs`
- Modify: `tools-dotnet/Utility/GenericErrorExtensions.cs`
- Modify: `tools-dotnet.Tests/PaginationTest/PaginationModelDeserializerTests.cs`
- Modify: `tools-dotnet.Tests/PaginationTest/PaginationProcessorTests.cs`
- Modify: `tools-dotnet.Tests/PaginationTest/PaginationErrorMappingTests.cs`
- Modify: `tools-dotnet.Tests/PaginationTest/PaginationSieveCompatibilityTests.cs`
- Modify: `tools-dotnet.Tests/PaginationTest/PaginationMsSqlIntegrationTests.cs`
- Modify: `tools-dotnet.Tests/PaginationTest/PaginationPostgresqlIntegrationTests.cs`

**Interfaces:**
- Produces: distinct machine-readable codes for unknown, not-filterable, and not-sortable fields
- Preserves: HTTP 400 `ApiPaginationError`
- Preserves: custom method fallback before unknown-field failure

- [ ] **Step 1: Add failing classification tests**

Assert missing filter syntax is `InvalidSyntax`, an unmapped field is
`UnknownField`, a mapped blocked filter is `FieldNotFilterable`, and a mapped
blocked sort is `FieldNotSortable`.

```csharp
exception.ErrorCode.ShouldBe(PaginationErrorCode.FieldNotFilterable);
exception.Field.ShouldBe("hidden");
```

- [ ] **Step 2: Verify RED**

Run:

```bash
dotnet test tools-dotnet.Tests/tools-dotnet.Tests.csproj --no-restore --filter "FullyQualifiedName~PaginationModelDeserializerTests|FullyQualifiedName~PaginationProcessorTests|FullyQualifiedName~PaginationErrorMappingTests"
```

Expected: classification tests FAIL because blocked and unknown fields currently
share `UnknownField`, and a missing field is misclassified.

- [ ] **Step 3: Implement member-resolution status**

Return an internal resolution result that distinguishes `Found`, `Unknown`,
`NotFilterable`, and `NotSortable`. Keep custom method lookup for `Unknown`
before throwing.

Add enum values:

```csharp
FieldNotFilterable,
FieldNotSortable,
```

Map each status to its corresponding exception factory.

- [ ] **Step 4: Complete API-error tests**

Cover filter and sort errors and all optional extensions:
`parameter`, `errorCode`, `field`, `value`, `operator`, and `targetType`.
Assert absent optional data is not serialized as an extension.

- [ ] **Step 5: Run provider integration tests**

Run:

```bash
dotnet test tools-dotnet.Tests/tools-dotnet.Tests.csproj --no-restore --filter "FullyQualifiedName~Pagination"
```

Expected: all pagination unit and SQL Server/PostgreSQL integration tests PASS.

- [ ] **Step 6: Commit**

```bash
git add tools-dotnet/Exceptions tools-dotnet/Errors/ApiPaginationError.cs tools-dotnet/Utility/GenericErrorExtensions.cs tools-dotnet/Pagination/Services tools-dotnet.Tests/PaginationTest
git commit -m "feat: classify invalid pagination requests"
```

### Task 5: Update Every Central NuGet Version

**Files:**
- Modify: `Directory.Packages.props`
- Modify only for compatibility: `tools-dotnet/tools-dotnet.csproj`
- Modify only for compatibility: `tools-dotnet.Tests/tools-dotnet.Tests.csproj`
- Modify only for compatibility: affected C# source and tests

**Interfaces:**
- Preserves: all existing `PackageReference` entries
- Produces: latest stable central versions as observed on 2026-07-23

- [ ] **Step 1: Update central versions**

Set:

```text
AutoMapper 16.2.0
BenchmarkDotNet 0.15.8
coverlet.collector 10.0.1
Enum.Ext 1.0.9
FluentAssertions 8.10.0
FluentValidation 12.1.1
JunitXml.TestLogger 8.0.0
Microsoft.AspNetCore.OpenApi 10.0.10
Microsoft.AspNetCore.WebUtilities 10.0.10
Microsoft.EntityFrameworkCore 10.0.10
Microsoft.EntityFrameworkCore.InMemory 10.0.10
Microsoft.EntityFrameworkCore.SqlServer 10.0.10
Microsoft.NET.Test.Sdk 18.8.1
Npgsql.EntityFrameworkCore.PostgreSQL 10.0.3
NUnit 4.6.1
NUnit.Analyzers 4.14.0
NUnit3TestAdapter 6.2.0
Serilog 4.4.0
Swashbuckle.AspNetCore.SwaggerGen 10.2.3
Shouldly 4.3.0
System.Text.Encodings.Web 10.0.10
System.Threading.Tasks.Dataflow 10.0.10
Testcontainers.PostgreSql 4.13.0
Testcontainers.MsSql 4.13.0
```

- [ ] **Step 2: Restore and build**

Run:

```bash
dotnet restore tools-dotnet.slnx
dotnet build tools-dotnet.slnx --no-restore
```

Expected: restore and build exit 0. Keep the three NU1510 warnings because the
user explicitly requires retaining those references.

- [ ] **Step 3: Resolve upgrade compatibility issues test-first**

For each behavioral break, add or isolate a failing focused test, verify the
failure, make the smallest compatibility change, and rerun that test.

- [ ] **Step 4: Verify dependency state**

Run:

```bash
dotnet list tools-dotnet.slnx package --outdated
dotnet list tools-dotnet.slnx package --vulnerable --include-transitive
```

Expected: no newer stable direct dependency and no known vulnerable dependency.

- [ ] **Step 5: Commit**

```bash
git add Directory.Packages.props tools-dotnet tools-dotnet.Tests
git commit -m "build: update stable NuGet dependencies"
```

### Task 6: Final Verification And Review

**Files:**
- Modify only if verification finds a scoped defect: files owned by Tasks 1-5

**Interfaces:**
- Verifies: build, tests, package output, dependency freshness, vulnerabilities, and final diff

- [ ] **Step 1: Run full tests**

```bash
dotnet test tools-dotnet.slnx --no-restore
```

Expected: all tests PASS with zero failures.

- [ ] **Step 2: Pack the library**

```bash
dotnet pack tools-dotnet/tools-dotnet.csproj --no-restore --output /tmp/tools-dotnet-pack
```

Expected: exit 0 and a `.nupkg` under `/tmp/tools-dotnet-pack`.

- [ ] **Step 3: Review warnings and diff**

```bash
git diff --check
git status --short
git diff --stat origin/sieve-depecation...HEAD
git diff --stat
```

Expected: no whitespace errors, no accidental package-reference removal, and no
unrelated production changes.

- [ ] **Step 4: Re-run freshness and vulnerability checks**

```bash
dotnet list tools-dotnet.slnx package --outdated
dotnet list tools-dotnet.slnx package --vulnerable --include-transitive
```

Expected: no outdated direct package and no vulnerable package.

- [ ] **Step 5: Commit final verification fixes if needed**

```bash
git add README.md Directory.Packages.props tools-dotnet tools-dotnet.Tests
git commit -m "chore: finish local changes hardening"
```

Skip this commit when verification requires no additional file changes.
