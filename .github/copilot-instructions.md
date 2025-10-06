````instructions
````instructions
<!--
This file is intended to help AI coding agents (and their human reviewers)
get productive quickly in this repository. It highlights the big-picture
architecture, important files, developer workflows (build/test/run),
project-specific conventions/patterns, integration points, and common
pitfalls.

Keep this file focused and short — prefer adding a small note and a
precise file path rather than repeating large blocks of code.
-->

# Copilot / AI agent instructions — PetPal Full-Stack

Purpose: give an AI code assistant the essential context needed to make
safe, useful edits in this full-stack pet management system (API + Client).
If you are making changes to code, prefer small, testable edits and run the
build/tests described below.

## High-level architecture (big picture)

**Full-Stack Structure**: This workspace contains both backend (`PetPal-api-rhyfxe/`) 
and frontend (`PetPal-client-rhyfxe/`) as sibling directories.

**Backend**: ASP.NET Core Minimal API (net8.0). Entry point is `PetPal.API/Program.cs` 
which wires services, maps endpoints, and handles sophisticated cookie-based auth 
with environment-specific CORS policies.

**Frontend**: Next.js 15 React app with Radix UI themes, TanStack Query for state 
management, and React Hook Form for form handling. Client uses cookie-based auth 
that integrates seamlessly with the backend.

**API Style**: Minimal API with endpoint mapping extensions in `PetPal.API/Endpoints/*` 
(each `MapXxxEndpoints` groups related routes). Follow this pattern when adding endpoints.

**Data Architecture**: EF Core with PostgreSQL. Complex ownership model where pets can 
have multiple owners via `PetOwner` join entity with `IsPrimaryOwner` flag. All DateTime 
properties auto-convert to UTC via `OnModelCreating` ValueConverter.

**Auth Flow**: ASP.NET Identity with simple cookie auth (SameSite=Lax, SecurePolicy=SameAsRequest). 
Three roles: `Admin`, `User`, `Veterinarian`.

**Cross-Component Communication**: Client services in `src/services/` mirror API endpoints. 
AuthContext provides app-wide auth state. API uses AutoMapper for DTO transformations.**Why Key Architectural Decisions Exist**:
- **Minimal API Pattern**: Keeps endpoints focused and testable. Authorization 
  logic lives in endpoint handlers, not separate controllers.
- **Pet Ownership Model**: `PetOwner` join entity supports multiple owners per 
  pet with primary/secondary distinction — critical for family pet management.
- **Address as Owned Type**: `Address` is owned by `UserProfile` (not separate table). 
  No `DbSet<Address>` — see `OnModelCreating` in `PetPalDbContext`.
- **UTC Enforcement**: All DateTime properties auto-convert to UTC via ValueConverter. 
  Always work with UTC dates.
- **Simple Cookie Auth**: Single policy with SameSite=Lax and SecurePolicy=SameAsRequest 
  works for both HTTP and HTTPS without environment complexity.

## Important files — start here

**Backend Core**:
- `PetPal.API/Program.cs` — DI container, sophisticated CORS policies, cookie 
  auth config, endpoint mapping, auto-migration on startup
- `PetPal.API/Data/PetPalDbContext.cs` — Complex relationships, owned types, 
  UTC DateTime converter, pet ownership model
- `PetPal.API/Endpoints/*.cs` — Follow `MapXxxEndpoints` extension pattern. 
  See `AuthEndpoints.cs` for user claim patterns, `PetEndpoints.cs` for 
  ownership checks
- `PetPal.API/Helpers/MappingProfiles.cs` — AutoMapper config with computed 
  properties (owner names, pet names in DTOs)

**Frontend Core**:
- `PetPal-client-rhyfxe/src/contexts/AuthContext.js` — App-wide auth state, 
  role-based access control, session restoration
- `PetPal-client-rhyfxe/src/services/` — API service layer mirrors backend 
  endpoints. Check `apiService.js` for cookie handling patterns
- `PetPal-client-rhyfxe/package.json` — Next.js 15, TanStack Query, Radix UI

**Data Layer**:
- `PetPal.API/Models/` and `PetPal.API/DTOs/` — Domain vs transfer objects
- `Migrations/` — Auto-applied on startup via `DbInitializer.Initialize()`

## Developer workflows (essential commands)

**Backend Setup** (from `PetPal-api-rhyfxe/`):
```bash
# One-time DB setup
cd PetPal.API
dotnet user-secrets init --project PetPal.API.csproj
dotnet user-secrets set 'ConnectionStrings:PetPalDbConnectionString' 'Host=localhost;Port=5432;Username=postgres;Password=your_password;Database=PetPal'

# Run backend (auto-migrates DB)
dotnet run  # Runs on http://localhost:5000
```

**Frontend Setup** (from `PetPal-client-rhyfxe/`):
```bash
npm install
npm run dev  # Runs on http://localhost:3000 with Turbopack
```

**Full-Stack Testing**:
```bash
# Backend tests (from PetPal-api-rhyfxe/)
dotnet test
dotnet test --collect:"XPlat Code Coverage"

# Frontend linting (from PetPal-client-rhyfxe/)
npm run lint
```

**Cookie Auth Notes**: Simple configuration uses SameSite=Lax and SecurePolicy=SameAsRequest 
for all environments. CORS allows localhost:3000 and localhost:5173 (both HTTP and HTTPS).

## Project-specific patterns (follow these)

**Backend Patterns**:
- **Endpoint Extensions**: Create `public static void MapXxxEndpoints(this WebApplication app)` 
  in `Endpoints/` and register in `Program.cs`. One resource per file.
- **Auth Claims**: Get user via `ClaimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier)`, 
  then load `UserProfile` by `IdentityUserId`. See `PetEndpoints.cs` examples.
- **Ownership Checks**: Most pet operations verify `IsPrimaryOwner` or any ownership 
  via `PetOwner` join table before allowing modifications.
- **EF Query Style**: Use `.Include().ThenInclude()` with conditional includes like 
  `Owners.Where(o => o.PetId == o.Pet.Id)`. Match existing query patterns.
- **DTO Mapping**: AutoMapper handles Model↔DTO with computed properties. Update 
  `MappingProfiles.cs` when changing shapes.

**Frontend Patterns**:
- **Service Layer**: Each API resource has a service file in `src/services/` 
  (e.g., `petService.js`, `authService.js`). These handle API calls and error handling.
- **Auth Flow**: `AuthContext` provides `user`, `login()`, `logout()`, and 
  role-checking functions. Components consume via `useAuth()`.
- **State Management**: TanStack Query for server state, React Context for auth/global state.
- **Form Handling**: React Hook Form for forms with validation patterns.

## Integration points & dependencies

**Database**: PostgreSQL via Npgsql. ASP.NET Identity tables live alongside app 
tables. `DbInitializer` seeds roles (`Admin`, `User`, `Veterinarian`) and test data.

**Backend Dependencies**: 
- EF Core 9.0 with PostgreSQL provider
- ASP.NET Identity for auth (UserManager/RoleManager for seeding)
- AutoMapper for DTO transformations
- xUnit + FluentAssertions + Moq for testing

**Frontend Dependencies**:
- Next.js 15 with Turbopack dev mode
- Radix UI for component primitives and theming
- TanStack Query for server state management and caching
- React Hook Form for form handling and validation

**Cross-Stack Communication**:
- Simple CORS policy allows localhost:3000 and localhost:5173 (HTTP/HTTPS)
- Cookie-based auth with credentials enabled
- Client services mirror API endpoints 1:1 (see `src/services/`)

## Tests and CI

- Tests: `PetPal.Tests` (xUnit). Use in-memory provider for unit tests
  and Postgres for CI integration tests. CI workflows exist to run
  tests and collect coverage; follow the example in
  `.github/workflows/` when adding checks.
- Coverage: the repository uses `coverlet` and produces XPlat code
  coverage in CI. Keep coverage thresholds in mind when changing
  public behavior.

## Common pitfalls & solutions

**Cookie Auth Issues**: Simple config works for all environments. If auth breaks, 
check browser dev tools for cookie details and ensure frontend uses credentials.

**Pet Ownership Bugs**: Always check ownership through `PetOwner` join table, 
not direct relationships. Many operations require `IsPrimaryOwner = true`.

**EF Query Issues**: Address is an owned type (not separate table). Use 
`.Include(u => u.Address)` on UserProfile, never query Address directly.

**DateTime Issues**: All dates auto-convert to UTC. Don't manually convert 
— let the ValueConverter handle it.

**Migration Problems**: `DbInitializer.Initialize()` runs on startup. If 
schema issues occur, manually run `dotnet ef database update`.

## AI agent workflow

1. **Understand the change**: Read relevant endpoint/service files first
2. **Follow patterns**: Use existing code as examples (especially auth and ownership checks)
3. **Test immediately**: Run `dotnet test` (backend) and `npm run lint` (frontend)
4. **Handle migrations**: When changing models, add migration and test locally
5. **Update mappings**: DTO/Model changes need AutoMapper profile updates
6. **Preserve auth flow**: Don't break cookie auth or role-based access control

## Testing credentials & useful snippets

**Seeded Admin Account** (created by `DbInitializer`):
- Email: `admin@petpal.com` 
- Password: `Admin123!`

**Common Query Patterns**:
```csharp
// Get user's pets with ownership info
var pets = await db.PetOwners
    .Include(po => po.Pet)
    .ThenInclude(p => p.Owners.Where(o => o.PetId == o.Pet.Id))
    .Where(po => po.UserProfileId == userProfileId)
    .Select(po => po.Pet)
    .ToListAsync();

// Check primary ownership
var isPrimaryOwner = await db.PetOwners
    .AnyAsync(po => po.PetId == petId && 
                   po.UserProfileId == userProfileId && 
                   po.IsPrimaryOwner);
```

**Frontend Auth Usage**:
```javascript
const { user, isAuthenticated, hasRole } = useAuth();
if (hasRole('Admin')) { /* admin features */ }
```