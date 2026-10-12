# Quickstart: MVP de Diagnósticos de Oportunidades Tecnológicas

This guide is the implementation target for local development. The application is for one consultant and must remain local or restricted until access control is decided.

## Prerequisites

- Stable .NET 10 SDK 10.0.302 (the repository pins it in `global.json`).
- PostgreSQL 18, running locally or in a restricted development environment.
- A current browser with WebAssembly and PWA installation support.
- The repository's local `dotnet-ef` tool manifest.

## Configure

1. Restore the pinned tool and project dependencies:

   ```sh
   dotnet tool restore
   dotnet restore
   ```

2. Create a local PostgreSQL database and a role for the app. For example, in `psql`:

   ```sql
   CREATE ROLE prisma_app LOGIN PASSWORD 'REPLACE_LOCALLY';
   CREATE DATABASE prisma OWNER prisma_app;
   ```

   Use a local development password; do not commit it.

3. Store the API connection string in User Secrets:

   ```sh
   dotnet user-secrets init --project src/Prisma.Api
   dotnet user-secrets set "ConnectionStrings:Prisma" "Host=localhost;Port=5432;Database=prisma;Username=prisma_app;Password=REPLACE_LOCALLY" --project src/Prisma.Api
   ```

   The connection string belongs to the API process and must not be copied into Blazor client settings. For a restricted deployment, supply it to the API process as `ConnectionStrings__Prisma` and configure `Cors__AllowedOrigins__0` with the exact allowed browser origin. For same-origin hosting, use the application's own origin.

4. Set the configured development client origin in API configuration so CORS accepts only that origin. When the published client is served by the API, the browser uses same-origin requests and needs no cross-origin exception.

## Apply database migrations

Apply the checked-in EF Core migrations explicitly:

```sh
dotnet ef database update --project src/Prisma.Api --startup-project src/Prisma.Api
```

When a schema change is intentionally made during development, add a migration and review the generated migration before applying it:

```sh
dotnet ef migrations add DescriptiveChange --project src/Prisma.Api --startup-project src/Prisma.Api
```

Do not apply migrations implicitly at application startup.

## Run locally

Run the API and the standalone Blazor WebAssembly development host in separate terminals:

```sh
dotnet run --project src/Prisma.Api
```

```sh
dotnet run --project src/Prisma.Client
```

The client API base address and the API's allowed development CORS origin match the local launch URLs (`http://localhost:5167` and `http://localhost:5173`). When served by the API, the client uses the current origin automatically. The API exposes the generated OpenAPI document only in Development.

The API project references the separate Blazor project for publish output and serves its static assets and SPA routes. To create a single restricted deployment:

```sh
dotnet publish src/Prisma.Api -c Release -o artifacts/publish
```

Configure the published API process with `ConnectionStrings__Prisma`, `Cors__AllowedOrigins__0` set to its own origin, and a loopback-only URL for local use. Start it from the publish directory so ASP.NET Core resolves `wwwroot` there:

```sh
cd artifacts/publish
ASPNETCORE_URLS=http://127.0.0.1:5167 dotnet Prisma.Api.dll
```

The API serves the client from `/`, serves client-side routes through `index.html`, and returns 404 for unknown `/api/*` routes. Bind locally or put the application behind a restricted network boundary; do not expose an unauthenticated instance openly to the internet.

The API's current `AllowedHosts` setting accepts only `localhost;127.0.0.1`. For a restricted deployment, override `AllowedHosts` in the API process with the exact hostname or hostnames clients will use, without a scheme or port. For example:

```sh
AllowedHosts='prisma.example.com;prisma-alt.example.com' ASPNETCORE_URLS=http://127.0.0.1:5167 dotnet Prisma.Api.dll
```

Separate multiple hostnames with semicolons and do not use `*` or wildcard hostnames. If a reverse proxy is used, restrict its accepted hostnames and preserve or forward the intended browser-request `Host` value to the API so it matches `AllowedHosts`.

## Test

Automated tests use a separate PostgreSQL test database, never the normal local `prisma` database. Set the `ConnectionStrings__PrismaTest` environment variable to a connection string whose database name is exactly `PrismaTest`; the test fixture rejects another database name. Then run:

```sh
dotnet test tests/Prisma.Api.Tests
```

The test suite focuses on the priority formula and tie-breaks, relationship ownership, blocked removals, status/completion rules, and the persisted API workflow. It does not add tests for simple properties.

## Primary acceptance path

1. Open the client and create a client with a company name, free-text segment, and two sectors.
2. Search for the client and create a diagnosis with a primary objective; verify the diagnosis starts as Draft and has “Melhorias rápidas”, “MVP”, and “Evolução” phases.
3. Link both sectors. Partially answer each fixed questionnaire and explicitly save. Leave one answer form with unsaved edits, choose to stay, then verify the values remain; save and reopen the diagnosis to verify persisted answers and counts.
4. Register three bottlenecks across the assessments. Create two opportunities, each linked to at least one bottleneck in this diagnosis. Verify score formula and tie-break ordering.
5. Reorder phases, assign some opportunities, and leave at least one unallocated. Verify phase order is manual and opportunity order is still priority-based.
6. Edit summary, recommendations, and next steps; explicitly save and reload the report.
7. Complete the diagnosis. Verify content edits are blocked; reopen it and verify the status is InProgress and saved data is preserved.
8. Print the report or choose the browser's Save as PDF. Verify navigation and controls are absent from print.
9. Install the PWA in a compatible browser. Disconnect from the API and verify data-changing actions report an error and do not claim success; no offline write is queued.

## README expectations

The repository README should remain short and point to this quickstart for prerequisites, configuration, running the API/client, and applying migrations.

## Validation evidence (2026-10-12)

- `dotnet build Prisma.sln --no-restore` succeeded with 0 warnings and 0 errors. `dotnet test tests/Prisma.Api.Tests --no-build` passed all 38 tests against the dedicated `PrismaTest` database.
- `dotnet ef database update` reported that the database was already up to date; `dotnet ef migrations has-pending-model-changes` reported no model changes since the last migration.
- The published service worker is configured to cache shell assets only. It excludes `/api` URLs and leaves non-GET requests to the network; the development service worker intentionally does not cache. The manifest declares standalone display and local icons. These were source checks; PWA installation was not confirmed in a compatible browser.
- A smoke in the Codex in-app browser rendered the client records and all eight report sections at a mobile viewport. Browser/version metadata was unavailable. Responsive report layout was observed; responsive data-entry forms were not confirmed.
- With the API stopped, browser interaction on the diagnosis form did not dispatch visible Blazor events (including the save button and objective checkbox). The failure UI and preservation of unsaved values could not be verified in that browser session. Code inspection shows the report only displays a success message after an API response confirms persistence, but this is not a browser/runtime confirmation.
- The report print stylesheet hides `.report-no-print`, navigation, and controls. The in-app browser print dialog did not complete, so the printed output was not visually confirmed.
- Release publish completed with 0 warnings and 0 errors. Running from `artifacts/publish` against the dedicated `PrismaTest` database served `/` and the `/clients` deep link as HTML (200), the manifest as `application/manifest+json` (200), and the fingerprinted WebAssembly bootstrap script as JavaScript (200). An unknown `/api/*` path returned Problem Details JSON with 404. These checks confirmed same-origin hosting; they did not verify PWA installation or browser printing.
