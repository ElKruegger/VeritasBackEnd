# CLAUDE.md — Véritas backend

Context for AI assistants (and humans) starting work on this repository. Read this first.

## What this is
ASP.NET Core (.NET 10) Web API for Véritas, a luxury menswear e-commerce store. Frontend (React + Vite, separate repo/folder) is not built yet. Product brief and running status live in the claude.ai Project "LuxuryStore" (`claude/status-and-next-steps.md`, `claude/backend-architecture.md`).

## Layout (Clean Architecture — dependencies point inwards only)
- `LuxuryStore.Domain` — entities (`BaseEntity` gives Id/CreatedAt/UpdatedAt), enums. No packages.
- `LuxuryStore.Application` — DTOs, `I*Service`/`I*Repository`/`IUnitOfWork` interfaces, services, `MappingExtensions` (hand-written, no AutoMapper), `Validation/` (FluentValidation). Never references Infrastructure or EF Core.
- `LuxuryStore.Infrastructure` — `AppDbContext`, `Persistence/Configurations` (one `IEntityTypeConfiguration<T>` per entity), `Migrations/`, repositories + `UnitOfWork`, `Security/TokenService`, `Storage/ImageUrlResolver`, `Persistence/Seed/CatalogSeeder`.
- `LuxuryStore-Backend` — controllers (thin, no try/catch), `Program.cs`, `Middleware/GlobalExceptionHandler`, `Middleware/OriginValidationMiddleware` (CSRF), `Filters/FluentValidationFilter`, `Authorization/Policies`.
- `LuxuryStore.UnitTests` — xUnit v3 + NSubstitute + Shouldly; folders mirror the layer under test (`Application/...`, `Infrastructure/...`). No database, no network.

## Commands (repo root)
```
dotnet build LuxuryStore-Backend.slnx
dotnet test                                                          # Microsoft.Testing.Platform via global.json
dotnet run --project LuxuryStore-Backend --launch-profile https      # https://localhost:7214, Scalar at /scalar
dotnet run --project LuxuryStore-Backend -- --seed                   # idempotent demo catalog
dotnet ef migrations add <Name> --project LuxuryStore.Infrastructure --startup-project LuxuryStore-Backend
dotnet ef migrations list   --project LuxuryStore.Infrastructure --startup-project LuxuryStore-Backend
dotnet ef database update   --project LuxuryStore.Infrastructure --startup-project LuxuryStore-Backend
```
Secrets via user-secrets in `LuxuryStore-Backend/`: `ConnectionStrings:DefaultConnection`, `Jwt:SigningKey` (>= 32 bytes).

## Conventions — follow these
- **Every migration that creates a table must also run `ALTER TABLE public."<Table>" ENABLE ROW LEVEL SECURITY;`** (Supabase exposes `public` through its Data API).
- Review every scaffolded migration: new NOT NULL columns get a meaningful `defaultValue` (EF scaffolds `""`), and data must be copied before a column is dropped.
- Never expose entities from controllers — DTOs only. Map in `MappingExtensions`.
- Validation lives in `Application/Validation` as `AbstractValidator<TDto>`; no DataAnnotations on DTOs. Max lengths must match the EF configuration.
- Services throw `NotFoundException` / `ConflictException` / `UnauthorizedException`; `GlobalExceptionHandler` maps them to 404/409/401 Problem Details.
- Repositories read with `AsNoTracking()`; services call `IUnitOfWork.SaveChangesAsync()` once per operation. Never `Include()` a navigation on an entity that is then passed to `Update()` (EF marks the whole graph Modified).
- Catalog writes use `[Authorize(Policy = Policies.AdminOnly)]`. Roles come from `Users.Role` (text). No endpoint grants Admin.
- Auth cookies are written/cleared only through `Controllers/AuthCookies.cs` (options must match for deletion to work).
- JWT uses short claim names (`sub`, `email`, `role`, `given_name`, `family_name`) with `MapInboundClaims = false`.
- Images: `ProductImages.Path` is a bucket object path or absolute https URL; `IImageUrlResolver` produces the public URL. Lowest `SortOrder` = primary image.
- Prices: `numeric(10,2)`, USD only. Server always recalculates totals — never trust client prices.
- Package versions: shared `Microsoft.Extensions.*` floors are floating (`[10.0.12,)`) to avoid NU1605 downgrade errors.
- Public-facing text (README, API messages, seed copy) is in English.
- Every new business rule or bug fix comes with a unit test. Tests are named as sentences (`Reusing_an_already_rotated_token_revokes_every_session_of_the_user`) and pass `TestHelpers.Ct` to async calls.
- Do not add VSTest-only packages (`Microsoft.NET.Test.Sdk`, `xunit.runner.visualstudio`, `coverlet.collector`) — VSTest is not supported for xUnit v3 on the .NET 10 SDK.
- CI (`.github/workflows/ci.yml`) must stay green: build, tests, and the NuGet vulnerability audit (fails on High/Critical). XML comments in `.csproj` files must not contain `--`.

## Git flow
Feature branches (e.g. `GeneralImplementation_0.1`) → `developer` → `main`. The repository owner handles commits and pushes.
