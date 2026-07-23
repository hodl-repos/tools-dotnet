# Local Changes Hardening Design

## Goal

Harden the recent local clock-provider and pagination work, improve its tests and
focused implementation quality, and update every existing NuGet reference to its
latest stable version, including major releases.

## Scope

The review boundary is:

- Commit `9cbf11d` (`added mockable timestamp provider`), which is ahead of the
  configured upstream branch.
- The current uncommitted pagination work, including the type-safe filter
  builder, invalid pagination exceptions and API errors, processor behavior,
  OpenAPI metadata, tests, and README changes.
- Dependency declarations in `Directory.Packages.props` and compatibility
  changes required by their upgrades.

Technical-debt cleanup is limited to code touched by these changes or code that
must change for dependency compatibility. Unrelated repository-wide refactors
are out of scope.

## Compatibility Policy

- Preserve existing public APIs unless a newly added local API is defective or
  unsafe.
- Keep every existing direct NuGet package reference. Restore warnings about
  redundant framework-provided references do not authorize their removal.
- Use the latest stable NuGet release for every existing centrally managed
  package, including major-version upgrades.
- Do not opt into prerelease package versions.
- Preserve custom pagination filter and sort method fallbacks.
- Invalid pagination input must fail predictably and map to a structured HTTP
  400 response.
- Explicit client sorting must continue to override attribute-based default
  sorting.

## Implementation Design

### Recent Change Review

Review the clock-provider commit and uncommitted pagination changes as public
library code. Look for behavioral regressions, ambiguous exception
classification, unsafe expression evaluation, unsupported operator/type
combinations, escaping and parsing asymmetry, API compatibility issues, and
duplicated test or implementation logic.

Defects found during review receive a regression test before their production
fix. Focused refactoring happens only after the relevant behavior is covered.

### Clock Provider

Verify that all production timestamp creation in the database context,
interceptor, and CRUD repositories uses `IClockProvider`. The system
implementation remains the default for backward compatibility, while tests use
a deterministic provider. Cover synchronous and asynchronous save paths,
created/updated timestamps, soft deletion, concurrent repositories, and key
wrapper repositories without relying on wall-clock timing.

### Pagination

Treat the builder, deserializer, processor, metadata, and API-error mapping as
one pipeline:

1. The type-safe builder converts member selectors and supported operations into
   the existing pagination wire format.
2. The deserializer parses that format or throws a specific pagination
   exception for malformed input.
3. The processor resolves allowed members or custom methods, converts values,
   validates operator compatibility, and applies filtering and sorting.
4. Pagination exceptions map to a stable HTTP 400 problem-details response.
5. OpenAPI metadata accurately describes filterable, sortable, and
   default-sorted properties.

Tests must cover all registered pagination operators, aliases, nested members,
captured values, nulls, escaping, invalid syntax, unknown or disallowed fields,
conversion failures, unsupported operator/type pairs, custom methods, default
sorting, and explicit-sort precedence. Provider-specific integration tests
verify SQL Server and PostgreSQL translation where behavior depends on EF Core.

### Dependency Updates

Query NuGet for latest stable direct versions, update
`Directory.Packages.props`, restore, and then resolve compile or runtime
compatibility changes. Existing references remain in both project files.

After upgrades, check direct and transitive vulnerability reports. A remaining
transitive advisory must be resolved through an existing top-level dependency
upgrade where possible; adding a new pin or reference requires evidence that
the owning top-level package cannot supply a fixed graph.

## Verification

Verification is performed from a clean restore result using the installed .NET
10 SDK:

- Build the solution with no compile errors.
- Run the full unit and integration test suite.
- Run focused pagination and clock-provider tests while iterating.
- Pack the library to verify package generation and public XML documentation.
- List outdated packages and confirm no existing direct reference has a newer
  stable release.
- Run NuGet vulnerability auditing for direct and transitive dependencies.
- Review the final diff for accidental formatting churn, unrelated changes, and
  public API regressions.

Existing analyzer or restore warnings are evaluated individually. Warnings
introduced by this work are fixed; unrelated pre-existing warnings are reported
without broad cleanup unless they block a successful build, test, or package.
