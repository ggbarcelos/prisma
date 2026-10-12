# Implementation Plan: MVP de Diagnósticos de Oportunidades Tecnológicas

**Branch**: `feature/001-opportunity-diagnoses` | **Date**: 2026-10-11 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification at `specs/001-opportunity-diagnoses/spec.md`

## Summary

Deliver the consultant's end-to-end workflow: register clients and sectors, create a diagnosis, save fixed sector questionnaires, record bottlenecks, link and prioritize opportunities, arrange optional implementation phases, edit the report, conclude or reopen the diagnosis, and print it from the browser. The implementation is a small .NET 10 monolith with a feature-organized Minimal API, separate Blazor WebAssembly PWA, PostgreSQL persistence through EF Core/Npgsql, and a contracts-only shared project.

## Technical Context

**Language/Version**: C# on .NET 10 LTS (`net10.0`); pin the available stable SDK `10.0.302` in `global.json`, with previews disabled.

**Primary Dependencies**: ASP.NET Core Minimal APIs, Blazor WebAssembly, Bootstrap, EF Core 10, Npgsql EF Core provider 10, and the aligned stable `dotnet-ef` 10 tool. Use platform validation and OpenAPI support before adding any third-party package.

**Storage**: PostgreSQL 18; EF Core migrations are the schema history and are applied explicitly.

**Testing**: xUnit and ASP.NET Core `WebApplicationFactory` for focused business-rule and API integration coverage against a dedicated PostgreSQL test database. The browser acceptance journey remains documented in [quickstart.md](quickstart.md).

**Target Platform**: Supported .NET 10 server runtime and current browsers with WebAssembly, service worker, and PWA installation support on compatible mobile, macOS, and Windows browsers.

**Project Type**: Monolithic web application with separate API and Blazor WebAssembly frontend projects plus a contracts-only shared project.

**Performance Goals**: No numerical throughput or latency target is defined for the single-consultant MVP. Keep normal CRUD interactions responsive and avoid work unrelated to the requested workflow.

**Constraints**: No login or multi-tenancy; deploy only locally or in a restricted environment. Data-changing operations require API connectivity. Cache installable application assets only; do not cache or queue API mutations. Keep secrets out of source and client bundles, and avoid sensitive customer information in logs. No external AI, file, audio, PDF-generation, or synchronization services.

**Scale/Scope**: One consultant, one application instance, and the seven P1 stories in the feature specification. No administrative configuration area or future-facing infrastructure.

## Constitution Check

**Before Phase 0 — PASS**

- The end-to-end MVP workflow and seven P1 stories are explicit in the spec.
- The required stack is preserved: C#/ASP.NET Core Minimal APIs, separate Blazor WebAssembly, Bootstrap, PostgreSQL, EF Core/Npgsql migrations, and a stable supported .NET release.
- The application remains a monolith organized by feature. The shared project is limited to cross-boundary DTOs/contracts; backend feature handlers use `DbContext` directly.
- The plan adds no authentication, public deployment, offline data synchronization, AI integration, server-side PDF generation, speculative infrastructure, or unrequested product capability.
- Input validation, save confirmation after API persistence, safe logging/configuration, and focused tests are included in the implementation approach.

**After Phase 1 design — PASS**

- The entity relations and API contracts preserve client/diagnosis/sector ownership and enforce same-diagnosis opportunity-to-bottleneck links.
- Completion and re-opening are explicit status transitions; concluded diagnoses are read-only, and phase assignment is optional.
- The PWA caches only its application shell. Database writes remain online and explicit.
- The proposed tests focus on rules and the main persisted flow; no test is planned for trivial accessors.
- No constitution exception or additional architectural layer is required.

## Project Structure

### Documentation (this feature)

```text
specs/001-opportunity-diagnoses/
├── plan.md
├── research.md
├── data-model.md
├── architecture.md
├── quickstart.md
└── contracts/
    └── openapi.yaml
```

`tasks.md` is intentionally deferred to `/speckit.tasks`.

### Source Code (repository root)

```text
global.json
Prisma.sln
README.md
Directory.Packages.props
.config/dotnet-tools.json
src/
├── Prisma.Api/
│   ├── Program.cs
│   ├── Data/
│   │   ├── AppDbContext.cs
│   │   └── Migrations/
│   ├── Api/
│   │   └── ProblemDetailsConfiguration.cs
│   └── Features/
│       ├── Clients/
│       ├── ClientSectors/
│       ├── Diagnoses/
│       ├── SectorAssessments/
│       ├── Bottlenecks/
│       ├── Opportunities/
│       ├── Phases/
│       └── Reports/
├── Prisma.Client/
│   ├── Program.cs
│   ├── Shared/
│   ├── Features/
│   │   ├── Home/
│   │   ├── Clients/
│   │   ├── Diagnoses/
│   │   ├── SectorAssessments/
│   │   ├── Bottlenecks/
│   │   ├── Opportunities/
│   │   ├── Phases/
│   │   └── Reports/
│   └── wwwroot/
│       ├── manifest.webmanifest
│       └── service-worker assets
└── Prisma.Contracts/
    ├── Clients/
    ├── ClientSectors/
    ├── Diagnoses/
    ├── SectorAssessments/
    ├── Bottlenecks/
    ├── Opportunities/
    ├── Phases/
    └── Reports/
tests/
└── Prisma.Api.Tests/
    ├── Features/
    └── Infrastructure/
```

**Structure Decision**: Use three application projects to reflect runtime and trust boundaries: API/persistence, browser client, and contracts. Keep API handlers, persistence access, and feature-specific validation beside each API feature; keep corresponding Blazor pages/forms/state beside each client feature. `Prisma.Contracts` contains only types crossing the HTTP boundary. The API process serves the published client assets for a restricted same-origin deployment; development may use separate origins with an explicitly configured CORS origin. Do not add a domain, application, infrastructure, repository, or mediator project.

## Complexity Tracking

No constitution violations. No exception is proposed.
