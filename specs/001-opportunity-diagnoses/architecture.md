# Feature Architecture: MVP de Diagnósticos de Oportunidades Tecnológicas

## Scope and references

This document describes the implementation boundaries for the feature, not additional product behavior.

- Specification: [spec.md](spec.md), stories US-001 through US-007, requirements FR-001 through FR-025.
- Plan: [plan.md](plan.md).
- API contract: [contracts/openapi.yaml](contracts/openapi.yaml).
- Data ownership and constraints: [data-model.md](data-model.md).
- Applicable local constitution: [../../.specify/memory/constitution.md](../../.specify/memory/constitution.md).
- Global Vertical Slice convention source: `~/.codex/memories/vertical-slice-conventions.md`.

## Architectural shape

Use one deployable ASP.NET Core 10 application process and one solution with three application projects:

1. `Prisma.Api` owns HTTP endpoints, feature validation, EF Core, migrations, and PostgreSQL access. It references `Prisma.Client` for publishing and serves its generated static web assets for a restricted same-origin deployment; the client remains a separate WebAssembly project.
2. `Prisma.Client` is a separate Blazor WebAssembly PWA. It owns Portuguese pages, forms, client-side validation, in-memory unsaved edits, route leave guards, report presentation, and calls to the API.
3. `Prisma.Contracts` contains only request/response DTOs and cross-boundary enum/contract types. It has no EF Core entities, UI components, or business service layer.

Within each runtime project, organize code by feature and use case. Map endpoints from `Program.cs` to small feature endpoint modules. Each endpoint validates its request, uses the injected `AppDbContext` directly, commits the operation, and maps a response. Keep feature-specific query and mapping code with those endpoints. Do not add MediatR, CQRS, generic repositories, domain/application/infrastructure projects, a message bus, or microservices.

## Stories and vertical slices

| Slice | Stories | Client entry point | API entry point and data ownership |
|---|---|---|---|
| Client and sector maintenance | US-001 | Home/client list, client editor, client sector editor | `/clients` and `/clients/{id}/sectors`; owns Client and ClientSector |
| Diagnosis setup and status | US-002, US-007 | Home, diagnosis editor, diagnosis navigation | `/diagnoses`, `/{id}`, `/{id}/status`; owns Diagnosis and DiagnosisObjective |
| Sector interview | US-003 | Assessment list and fixed questionnaire form | `/{id}/assessments` and `/{assessmentId}/answers`; owns SectorAssessment and its fixed response columns |
| Bottleneck investigation | US-004 | Diagnosis bottleneck list/editor | `/{id}/bottlenecks`; owns Bottleneck |
| Opportunity assessment | US-005 | Diagnosis opportunity list/editor | `/{id}/opportunities`; owns Opportunity and OpportunityBottleneck |
| Phased plan | US-006 | Diagnosis plan editor | `/{id}/phases` and `/{id}/phases/order`; owns Phase; opportunities retain their optional phase FK |
| Report review and print | US-007 | Report editor/view and browser print action | `/{id}/report`; projects the saved diagnosis graph and owns only the editable report text on Diagnosis |

The API uses a single `AppDbContext`; slices do not call one another through a service bus. A diagnosis create operation writes its three suggested Phase rows in the same database transaction. Shared validation is limited to small request/state checks where needed; data access remains visible at each feature boundary.

## Data ownership and integrity

- Client owns its sectors and diagnoses. The report reads the current client name and profile fields; each linked sector label is snapshotted on `SectorAssessment`. Update a client's `UpdatedAtUtc` when the profile or one of its sectors changes. Update a diagnosis's `UpdatedAtUtc` with every successful mutation in that diagnosis so recent lists are accurate.
- Diagnosis owns its objectives, sector assessments, bottlenecks, opportunities, and phases.
- Opportunity owns only the explicit link rows to one or more bottlenecks and an optional same-diagnosis phase reference.
- Composite database foreign keys enforce same-client and same-diagnosis relationships. Request handlers also verify ownership and return a conflict or not-found response without leaking unrelated record details.
- Database constraints enforce unique assessment links, at most one primary objective, unique phase order within a diagnosis, and valid score input ranges. API rules enforce nonempty opportunity-to-bottleneck links and completion prerequisites in the same write transaction.
- The report is a projection over saved rows. Do not generate or persist a second report snapshot or derived priority score.

## Status, writes, and unsaved edits

- `Draft ↔ InProgress` is a normal status edit.
- `Draft` or `InProgress` may transition to `Completed` only with a selected primary objective, at least one bottleneck, and at least one opportunity.
- `Completed → InProgress` is the explicit reopen action. A completed diagnosis rejects content writes until reopened; it cannot transition directly back to Draft.
- Phase assignment and fully answered interviews are not completion requirements.
- Each form keeps its unsaved values in component memory and sends a request only on an explicit save action. A route leave guard asks before discarding; choosing to stay retains the current values. A browser unload guard uses the browser prompt where supported.
- A successful save state is displayed only after the API has committed and returned success. On a failed request, keep the form open and its values intact. Do not automatically retry writes. If the outcome is uncertain after a connection loss, allow the client to re-fetch the relevant list/detail before the user retries; never claim persistence without confirmation.

## Contracts and dependencies

- Browser-to-API traffic is JSON over HTTP as specified by [contracts/openapi.yaml](contracts/openapi.yaml). The API returns Problem Details, including field errors, for invalid requests and conflicts.
- Serialize contract enum codes as stable English strings; the UI maps those codes to Brazilian Portuguese labels.
- Client and server both validate required values, enum/range constraints, objective selection, and fixed response fields. The API is authoritative.
- EF Core/Npgsql 10 uses explicit migrations. Application startup does not mutate the schema.
- Keep direct package versions in root `Directory.Packages.props` and the local `dotnet-ef` version in `.config/dotnet-tools.json` so EF Core, Npgsql, and the CLI stay aligned.
- Runtime dependencies are ASP.NET Core, Blazor WebAssembly, Bootstrap, EF Core/Npgsql, and the aligned EF CLI tool. Avoid an additional validation, mapping, mediation, or UI component framework.
- The service worker caches installable shell assets only. It does not cache API responses, queue requests, or store form/database data for offline synchronization.
- There are no LLM, email, file storage, audio, PDF renderer, or other external service effects.

## Security and deployment boundaries

- There is no login or user/tenant model. The intended initial deployment binds locally or is otherwise restricted. Do not expose the application openly to the internet until an access-control decision is made.
- In local development, allow only the configured Blazor origin through CORS. In restricted same-origin deployment, publish `Prisma.Api` so the referenced client assets are included in the server's `wwwroot`; the API serves static files and routes non-file paths to the SPA entry point. Unknown `/api/*` routes remain 404 responses.
- Store database connection settings in User Secrets or environment variables. Do not put secrets in Blazor configuration, committed JSON, or published browser assets.
- Logs may contain route, operation name, outcome, and correlation identifiers. Do not log client names, contact details, interview responses, bottleneck text, opportunity notes, report content, request bodies, or full connection strings.
- No auth cookies are used. There is no cross-client/tenant data boundary beyond the single local/restricted deployment and relational ownership constraints.

## Failure and retry behavior

- Validate before mutating; commit each form's aggregate changes in one database transaction where more than one row is involved.
- Translate missing records to 404, invalid input to 400, and invalid status transitions, read-only writes, cross-owner links, and blocked removals to 409 Problem Details.
- Database constraint failures are translated to a safe conflict response; raw SQL, connection strings, and submitted client data are not returned to the UI or written to logs.
- A database/network failure returns a non-success response when possible. The client keeps user-entered form values and shows an error. It does not show save confirmation.
- Do not run transparent retries for POST or other writes. After a transport failure with an uncertain server outcome, the UI can refresh the affected collection/detail while retaining in-memory inputs; the user decides whether to retry.
- API unavailability prevents create/update operations. There is no offline write queue, persistence fallback, or background synchronization.

## Validation and verification

- Use Blazor form validation to identify issues while editing; repeat all business validation in the API so direct requests cannot bypass it.
- Focus automated tests on formula and ordering, same-diagnosis association constraints, assessment removal guards, status transitions/completion prerequisites, and the persisted primary API journey using PostgreSQL.
- Use the quickstart manual path for the responsive browser experience, save/leave confirmation, PWA installation, and printable output.

## Technical decisions

- Stable .NET 10 and the aligned EF Core/Npgsql 10 package line: see [research.md](research.md).
- Fixed questionnaire answers are ten nullable columns on SectorAssessment; question definitions are static client content.
- Priority is computed from the three input values when queried/projected.
- The three suggested phases are inserted alongside a new diagnosis and remain ordinary editable phase rows.
- PostgreSQL composite foreign keys back up handler ownership checks so an invalid cross-diagnosis association cannot be persisted.
