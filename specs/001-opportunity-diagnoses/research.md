# Research: MVP de Diagnósticos de Oportunidades Tecnológicas

**Date**: 2026-10-11

This research resolves implementation choices left open by the specification. It does not add product scope.

## Decisions

### Runtime and package line

- **Decision**: Target `.NET 10 LTS` and `net10.0`. Pin the stable SDK `10.0.302` available in the current environment in `global.json` and set `allowPrerelease` to `false`.
- **Rationale**: .NET 10 is the current supported LTS line on the planning date. .NET 8 and .NET 9 are close to end of support, and .NET 11 is still a release candidate, so neither is a better basis for a new stable MVP. The available environment has a stable .NET 10 SDK.
- **Package alignment**: Use stable major version 10 for EF Core, Npgsql EF Core provider, and `dotnet-ef`; use the latest compatible servicing patch when implementation begins. Keep EF/Npgsql packages on the same supported major line and pin package/tool versions in central package/tool manifests rather than using floating versions. Do not use previews.

### PostgreSQL

- **Decision**: Target the supported PostgreSQL 18 major line and use a current stable 18.x minor release for development and deployment.
- **Rationale**: PostgreSQL 18 is stable and has a longer support window than the older supported majors. PostgreSQL 19 is beta on the planning date and is excluded.

### API, client, and deployment

- **Decision**: Use feature-grouped ASP.NET Core Minimal API route handlers with JSON request/response contracts. Keep the Blazor WebAssembly client in a separate project. Publish the client assets with the API for the restricted single-origin deployment; use separate local development origins with CORS limited to the configured client origin.
- **Rationale**: This preserves the required runtime boundary while keeping the MVP to one deployable application process. Same-origin production hosting avoids unnecessary CORS configuration. No new service or deployment platform is required.
- **API errors**: Return ASP.NET Core Problem Details for validation, missing-resource, and conflict errors. Use 400 for invalid input, 404 for a missing resource, and 409 for invalid state changes or cross-diagnosis relationships.
- **API description**: Keep the internal JSON API described in the feature's OpenAPI 3.1 contract. It is a client/server contract, not a public access policy.

### Persistence and business rules

- **Decision**: Use one EF Core `AppDbContext` in the API. Feature endpoints use it directly. Apply migrations explicitly; do not migrate the database during application startup.
- **Rationale**: The single-consultant workload needs no repository abstraction, mediator, or background processing. Explicit migration application makes schema changes reviewable and startup behavior predictable.
- **Relationship integrity**: Use foreign keys and unique constraints for same-client sectors, same-diagnosis sector assessments, and same-diagnosis opportunity/bottleneck links. Validate multi-row requirements (at least one objective for completion and at least one bottleneck per opportunity) in the API within the write transaction.
- **Default phases**: Create the three suggested phases (“Melhorias rápidas”, “MVP”, “Evolução”) with the new diagnosis in the same transaction. They are ordinary editable phases, not a separate template system.
- **Priority**: Calculate score when projecting opportunities using `(Impact × 2) + Urgency − Effort`. Keep no stored score that could drift from its inputs. Sort by score descending, impact descending, effort ascending, title ascending; use ID as a final deterministic tie-break only if titles also match.

### PWA and connectivity

- **Decision**: Configure a web app manifest and service worker so compatible browsers can install the application. Cache only the static application shell. Keep all `/api` calls network-only, with no offline queue, local CRUD store, or synchronization.
- **Rationale**: Installation and shell caching provide the requested PWA behavior without implying offline data access. A failed save leaves the current form values in memory and reports that persistence was not confirmed.

### Validation and tests

- **Decision**: Use built-in Blazor form/data-annotation validation for immediate feedback and explicit request validation in Minimal API handlers before writes. Return field-level validation details through Problem Details extensions.
- **Rationale**: The same rules must be enforced at both trust boundaries, and the existing stack has the required mechanisms without another validation dependency.
- **Tests**: Use xUnit and `WebApplicationFactory` for important business rules and the persisted API workflow with a dedicated PostgreSQL test database. Keep a browser-level manual acceptance path for responsive behavior, PWA installation, dirty-form navigation, and printing; do not introduce a browser automation framework in the MVP plan.
- **Logging**: Log operation, route, status, and correlation data only. Do not log request bodies, contact information, interview answers, bottleneck evidence, or report text.

## Considered alternatives

| Concern | Selected option | Alternative not selected | Reason |
|---|---|---|---|
| Runtime | .NET 10 LTS | .NET 8/9 or .NET 11 RC | Near-term end of support for 8/9; 11 is preview on the planning date. |
| Feature access | Minimal API handlers with direct `DbContext` | MediatR/CQRS/repository layers | Adds dependencies and indirection without an MVP requirement. |
| API validation | Platform validation plus small feature-local checks | Third-party validation framework | The fixed request set does not justify another package. |
| PWA behavior | Installable shell, API online-only | Offline CRUD and synchronization | Explicitly out of scope and adds conflict behavior. |
| Report output | HTML report and browser print | Server PDF renderer | Browser print/save-as-PDF is the required, simpler path. |
| Test infrastructure | xUnit integration tests with PostgreSQL | Separate ephemeral database platform or browser automation stack | Keep test infrastructure aligned with production storage and avoid an additional service/framework. |

## Sources

- [.NET support policy](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core) — support lifecycles for .NET 10, .NET 8/9, and the preview line.
- [PostgreSQL versioning and support](https://www.postgresql.org/support/versioning/) — supported majors and stable releases.
- [Npgsql EF Core 10 release notes](https://www.npgsql.org/efcore/release-notes/10.0.html) — provider 10.0 support for EF Core 10.
- [Host and deploy Blazor WebAssembly](https://learn.microsoft.com/en-us/aspnet/core/blazor/host-and-deploy/webassembly/?view=aspnetcore-10.0) — static/hosted deployment model.
- [Blazor Progressive Web App](https://learn.microsoft.com/en-us/aspnet/core/blazor/progressive-web-app?view=aspnetcore-10.0) — installable PWA manifest and service worker behavior.
