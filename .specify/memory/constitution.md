<!--
Sync Impact Report
- Version change: unratified template → 1.0.0 (initial adoption).
- Modified principles: template principle 1 → I. End-to-End MVP Delivery;
  template principle 2 → II. Simplicity and Focus;
  template principle 3 → III. Client Data Protection;
  template principle 4 → IV. Reliable User Experience;
  template principle 5 → V. Relevant Quality Validation.
- Added sections: Required Technology Stack and Architecture;
  Product Scope and Development Workflow.
- Removed sections: none; template examples and placeholders were replaced.
- Follow-up TODOs: none.
-->

# Prisma Constitution

## Core Principles

### I. End-to-End MVP Delivery

Prioritize a usable, complete consultant workflow: record client information, investigate
operational pain points, and produce a diagnosis with opportunities for software, automation,
and AI. Deliver this flow before adding advanced capabilities. This keeps the MVP useful while
protecting implementation speed.

### II. Simplicity and Focus

Choose the simplest design that satisfies the approved scope. Avoid premature abstractions,
unnecessary dependencies, unrequested features, and infrastructure for hypothetical future
needs. Resolve reversible decisions autonomously and document relevant technical choices briefly.

### III. Client Data Protection

Do not write sensitive client information to logs. Keep configuration and secrets outside source
code. The initial application is for one consultant in a local or restricted environment; do
not expose it openly on the internet until access control is decided and implemented.

### IV. Reliable User Experience

Validate input in both the frontend and backend. The interface MUST show clear loading, error,
empty-list, and save-confirmation states. A save MUST NOT be reported as successful until the API
confirms persistence.

### V. Relevant Quality Validation

Test important business rules and the primary user flow. Keep tests focused on observable
behavior; do not add redundant tests for simple getters or setters. Keep the product usable and
its implementation understandable as the MVP evolves.

## Required Technology Stack and Architecture

- Backend: C# and ASP.NET Core, using Minimal APIs organized by feature.
- Frontend: a separate Blazor WebAssembly project, with Bootstrap for visual components and
  responsive behavior.
- Persistence: PostgreSQL with Entity Framework Core, the Npgsql provider, and migrations.
- Shared code: a shared project may contain only DTOs and contracts needed across the frontend
  and backend.
- Runtime and packages: select a stable, supported .NET version compatible with the environment;
  align package versions and do not use preview versions.
- Runtime shape: keep the application monolithic. Feature handlers use `DbContext` directly.
  Do not add MediatR, CQRS, generic repositories, microservices, an event bus, or multiple Clean
  Architecture layers.
- Feature architecture: maintain `specs/<feature>/architecture.md` for each feature. Organize
  business code around feature workflows. Each document records its scope, related spec and
  stories, slices and entry points, data ownership, security boundaries, dependencies, contracts,
  external effects, failure and retry behavior, validation, and relevant technical decisions.
  Include working relative links to the feature specification and applicable local constitution
  or `AGENTS.md`.
  Keep it consistent with the global Vertical Slice convention at
  `~/.codex/memories/vertical-slice-conventions.md`.

## Product Scope and Development Workflow

- The PWA MUST be installable in compatible mobile, macOS, and Windows browsers. Creating or
  changing data requires a connection to the API; offline data synchronization is out of scope.
- Initial scope excludes login, users, roles, multi-tenancy, LLM integration, audio recording,
  transcription, file uploads, and server-side PDF generation.
- Reports use browser printing and the browser's save-as-PDF option.
- User-facing text MUST be in Brazilian Portuguese. Source code and technical identifiers MUST
  be in English.
- Feature specifications MUST keep user value and scope explicit. Plans, tasks, and the feature
  architecture document MUST remain aligned with the constitution and with each other.
- The README MUST briefly explain configuration, running the application, and applying database
  migrations.
- Ask for clarification only when missing information prevents a correct implementation. If a
  decision is reversible and within scope, use the simplest suitable option.

## Governance

This constitution governs feature specifications, architecture, implementation, and validation.
Review each feature against it during specification and before delivery. A lasting exception or
scope expansion requires a documented project decision and an update to the relevant specification
or constitution before implementation.

Amendments MUST be made in this file and use semantic versioning: MAJOR for incompatible changes
to existing principles, MINOR for new or materially expanded principles or sections, and PATCH
for clarifications that do not change project obligations. Record the reason in a Sync Impact
Report at the top of the file. The project owner approves changes to product scope and governance.

**Version**: 1.0.0 | **Ratified**: 2026-10-11 | **Last Amended**: 2026-10-11
