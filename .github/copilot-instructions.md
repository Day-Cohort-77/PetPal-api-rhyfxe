<!--
This file is intended to help AI coding agents (and their human reviewers)
get productive quickly in this repository. It highlights the big-picture
architecture, important files, developer workflows (build/test/run),
project-specific conventions/patterns, integration points, and common
pitfalls.

Keep this file focused and short — prefer adding a small note and a
precise file path rather than repeating large blocks of code.
-->

# Copilot / AI agent instructions — PetPal API

Purpose: give an AI code assistant the essential context needed to make
safe, useful edits in this repo (server + client workspace). If you are
making changes to code, prefer small, testable edits and run the
build/tests described below.

## High-level architecture (big picture)

- Backend: ASP.NET Core Minimal API (net8.0). The program entry is
  `PetPal.API/Program.cs` which wires services and maps endpoints.
- API style: Minimal API with endpoint mapping extension methods under
  `PetPal.API/Endpoints/*` (each `MapXxxEndpoints` extension groups
  related routes). Prefer adding new endpoints in that folder and
  following the existing extension-pattern.
- Data layer: EF Core (PostgreSQL provider `Npgsql`) with `PetPalDbContext`
  at `PetPal.API/Data/PetPalDbContext.cs`. Models live in
  `PetPal.API/Models/` and DTOs in `PetPal.API/DTOs/`.
- Auth: ASP.NET Identity backed by `PetPalDbContext`. Cookie
  authentication is used (configured in `Program.cs`). Roles seeded:
  `Admin`, `User`, `Veterinarian`.
- Object mapping: AutoMapper configured in `Program.cs` and mapping
  profiles live in `PetPal.API/Helpers/MappingProfiles.cs`.
- DB initialization & seeding: `PetPal.API/Data/DbInitializer.cs`.
  Program calls `DbInitializer.Initialize(...)` at startup which runs
  `context.Database.Migrate()` and seeds roles/users/sample data.

Why some structural decisions exist
- Minimal API keeps controllers light and groups related routes as
  extension methods. This simplifies small feature additions but means
  routing logic and authorization checks sit near the route handlers.
- `Address` is modelled as an owned type on `UserProfile` (not a table)
  — see `OnModelCreating` in `PetPalDbContext`. This affects migrations
  and queries (no DbSet<Address>).
- All DateTime properties are converted to UTC in `OnModelCreating`
  using a ValueConverter. When creating/updating timestamps respect UTC.

## Important files and where to look first
- `PetPal.API/Program.cs` — DI, auth, CORS, JSON options, calls to map
  endpoints and DB initialization.
- `PetPal.API/Data/PetPalDbContext.cs` — EF Core model configuration
  (relationships, owned types, UTC conversion).
- `PetPal.API/Data/DbInitializer.cs` — migrations + seeding logic.
- `PetPal.API/Endpoints/*.cs` — endpoint implementations. Follow the
  `MapXxxEndpoints` pattern when adding routes.
- `PetPal.API/DTOs/` and `PetPal.API/Models/` — DTO and domain shapes.
- `PetPal.API/Helpers/MappingProfiles.cs` — AutoMapper mappings.
- `Migrations/` — existing EF migrations; `DbInitializer` runs
  `context.Database.Migrate()` automatically on startup.

## Developer workflows (quick commands)

Notes: your shell is `bash.exe` on Windows. Commands below assume you
run them from repo root `PetPal-api-rhyfxe`.

Set user secrets (one-time per machine/project):
```bash
cd PetPal.API
dotnet user-secrets init --project PetPal.API.csproj
dotnet user-secrets set 'ConnectionStrings:PetPalDbConnectionString' 'Host=localhost;Port=5432;Username=postgres;Password=your_password;Database=PetPal' --project PetPal.API.csproj
```

Apply migrations (you can also start the app; startup runs migrations):
```bash
cd PetPal.API
dotnet ef database update --project PetPal.API.csproj
```

Run the API locally:
```bash
cd PetPal.API
dotnet run
# or run from the solution with an IDE's debug launcher
```

Run tests (all projects):
```bash
dotnet test
dotnet test --collect:"XPlat Code Coverage"
# run only server tests (if needed)
dotnet test PetPal.Tests/PetPal.Tests.csproj
```

Notes on local HTTPS / cookies: the app configures cookies with
SameSite=None and SecurePolicy=Always. That requires the client to use
HTTPS for cookies to be honored. Either run the API with HTTPS or
adjust cookie settings when developing locally (quick temporary change
— but document it in PRs).

## Project-specific conventions & patterns

- Endpoint pattern: create an extension static class with
  `public static void MapXxxEndpoints(this WebApplication app)` and call
  it from `Program.cs` (e.g. `app.MapPetEndpoints()`). Keep each file
  focused on one resource area.
- Authorization: endpoints often call `ClaimsPrincipal user` to find
  `ClaimTypes.NameIdentifier`, then load the `UserProfile` by
  `IdentityUserId`. Follow the same pattern to check ownership.
- Pet ownership: `PetOwner` is a join entity with `IsPrimaryOwner`.
  Many handlers check primary ownership before allowing updates/deletes.
- EF Include patterns: code uses `.Include(...).ThenInclude(...)` and
  sometimes `Where` inside `Include` (e.g. `Owners.Where(o => o.PetId == o.Pet.Id)`).
  Keep queries consistent with the existing style when modifying.
- UTC times: always store and compare DateTime values as UTC. The
  DbContext enforces UTC conversion for DateTime properties.

## Integration points & external dependencies

- PostgreSQL via Npgsql (NuGet package `Npgsql.EntityFrameworkCore.PostgreSQL`).
  CI uses PostgreSQL for integration tests; locally you may run a
  Postgres container or installed service.
- ASP.NET Identity stores users and roles in the same DB (migrations
  include Identity tables). Seeding uses `UserManager` & `RoleManager`.
- AutoMapper maps DTOs <-> Models. Editing shapes may need mapping
  configuration updates (`MappingProfiles.cs`).
- Client interaction: this repo contains a client folder (`PetPal-client-rhyfxe`).
  The backend expects cookie auth and CORS policy `AllowLocalhost` that
  allows `http://localhost:3000` and `http://localhost:5173` and allows credentials.

## Tests and CI

- Tests: `PetPal.Tests` (xUnit). Use in-memory provider for unit tests
  and Postgres for CI integration tests. CI workflows exist to run
  tests and collect coverage; follow the example in
  `.github/workflows/` when adding checks.
- Coverage: the repository uses `coverlet` and produces XPlat code
  coverage in CI. Keep coverage thresholds in mind when changing
  public behavior.

## Common pitfalls and quick fixes

- Cookie HTTPS mismatch: SecurePolicy=Always + SameSite=None requires
  HTTPS. If cookies are missing in local dev, either run backend over
  HTTPS, modify cookie settings for local dev, or run the client over
  https.
- Database not migrated: `DbInitializer` calls `context.Database.Migrate()`
  at startup, but if migrations aren't applied you can run
  `dotnet ef database update` manually.
- Missing user profile after Identity user creation: several endpoints
  create a minimal profile for Admin users if missing. If tests fail
  due to missing profiles, inspect seeding in `DbInitializer`.

## How an AI agent should make changes

1. Read the small list of relevant files above before editing.
2. Keep changes minimal and explicit; prefer endpoint-level changes
   over sweeping framework edits.
3. Run unit tests locally after making changes (`dotnet test`). Fix
   compilation or test failures before proposing a PR.
4. When adding database schema changes: add EF migration, include the
   generated migration file under `Migrations/`, and verify migration
   applies locally (`dotnet ef database update`).
5. If changing DTOs or models, update AutoMapper profiles and tests.

## Useful snippets (for human reviewers)

- Seeded admin credentials (useful when testing):
  - Email: `admin@petpal.com`
  - Password: `Admin123!`
  These are created by `DbInitializer` when the database is empty.

## Where to add more documentation

- Add component-level notes near `Program.cs` or the relevant
  `Endpoints` file when you change behavior (e.g., change cookie
  policy, add a new role, change time handling).

---

If anything in this file is unclear, or you need runtime secrets,
explain what you need and why before modifying secrets or CI configs.