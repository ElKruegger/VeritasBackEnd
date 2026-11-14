# CLAUDE.md — Véritas backend

Context for AI coding agents (and humans) working on this repository. Read this first.

## Product
Véritas is a portfolio-grade luxury menswear and lifestyle e-commerce platform (tailoring, footwear, watches, accessories, fragrances), with an editorial aesthetic comparable to Mr Porter. All code, commits, docs, API messages and seed copy are in **English**. Single currency (USD), no i18n.

Stack: ASP.NET Core Web API on .NET 10 (this repo) · React + TypeScript + Vite + Tailwind + Zustand frontend (not started, separate repo) · PostgreSQL on Supabase · Supabase Storage for product images · Stripe Checkout (planned) · MailKit SMTP (planned) · Docker image deployed to Render · frontend on Vercel · GitHub Actions for CI/CD.

## Current state
Done: catalog (brands, categories, products, variants, images) with filters and paging · auth (register, login, refresh-token rotation with reuse detection, logout, me) in httpOnly cookies · roles (`Customer`/`Admin`) with an `AdminOnly` policy on every catalog write · FluentValidation · CORS · CSRF origin validation · rate limiting on auth · Serilog · RLS on every table · idempotent demo seed (27 products) · 87 unit tests · CI workflow · Dockerfile + docker-compose.

In progress: CD to Render (branch `feature_VeritasDockerCD`).

Roadmap, in order:
1. CD: GitHub Actions builds the Docker image, runs it with `--migrate` against Supabase, then triggers the Render deploy. Deploy on merge to `main` only.
2. Catalog gaps for the frontend: size/variant filter, related products, sort options.
3. Address endpoints → Cart (guest cart merges on login, server-side totals) → Orders + Stripe Checkout (webhook with signature verification, idempotency key, checkout rate limit) → order history → password reset via email.
4. Integration tests (WebApplicationFactory + Testcontainers PostgreSQL).
5. Later: Redis (HybridCache for the catalog, idempotency keys), RabbitMQ with the Outbox pattern and a Node.js worker for order emails.
Open security backlog: security headers (HSTS, nosniff, frame-ancestors), account lockout, user-enumeration hardening, purge of expired refresh tokens, least-privilege DB role.

## Layout (Clean Architecture — dependencies point inwards only)
- `LuxuryStore.Domain` — entities (`BaseEntity` gives Id/CreatedAt/UpdatedAt), enums. No packages.
- `LuxuryStore.Application` — DTOs, `I*Service`/`I*Repository`/`IUnitOfWork` interfaces, services, `MappingExtensions` (hand-written, no AutoMapper), `Validation/` (FluentValidation). Never references Infrastructure or EF Core.
- `LuxuryStore.Infrastructure` — `AppDbContext`, `Persistence/Configurations` (one `IEntityTypeConfiguration<T>` per entity), `Migrations/`, repositories + `UnitOfWork`, `Security/TokenService`, `Storage/ImageUrlResolver`, `Persistence/Seed/CatalogSeeder`.
- `LuxuryStore-Backend` — controllers (thin, no try/catch), `Program.cs`, `Middleware/GlobalExceptionHandler`, `Middleware/OriginValidationMiddleware` (CSRF), `Filters/FluentValidationFilter`, `Authorization/Policies`.
- `LuxuryStore.UnitTests` — xUnit v3 + NSubstitute + Shouldly; folders mirror the layer under test. No database, no network.

## Commands (repo root)
```
dotnet build LuxuryStore-Backend.slnx
dotnet test                                                          # Microsoft.Testing.Platform via global.json
dotnet run --project LuxuryStore-Backend --launch-profile https      # https://localhost:7214, Scalar at /scalar
dotnet run --project LuxuryStore-Backend -- --migrate --seed         # one-shot: apply migrations, load demo catalog, exit
dotnet ef migrations add <Name> --project LuxuryStore.Infrastructure --startup-project LuxuryStore-Backend
dotnet ef migrations list      --project LuxuryStore.Infrastructure --startup-project LuxuryStore-Backend
```
Docker (local API + throwaway PostgreSQL, isolated from Supabase):
```
docker compose up -d db
docker compose run --rm api --migrate --seed
docker compose up -d --build api          # http://localhost:8080/scalar
docker compose logs -f api
docker compose down                       # add -v to delete the database volume
```
Local secrets live in user-secrets of `LuxuryStore-Backend/`: `ConnectionStrings:DefaultConnection`, `Jwt:SigningKey` (>= 32 bytes). Never read, print or commit them.

## Definition of done
A change is finished only when `dotnet build LuxuryStore-Backend.slnx` has 0 errors and `dotnet test` passes. Say which commands you ran and their result. If a change affects the Docker image or `Program.cs` startup, also run the docker compose steps above.

## Conventions — follow these
- **Every migration that creates a table must also run `ALTER TABLE public."<Table>" ENABLE ROW LEVEL SECURITY;`** (Supabase exposes `public` through its Data API).
- Review every scaffolded migration: new NOT NULL columns get a meaningful `defaultValue` (EF scaffolds `""`), and data must be copied before a column is dropped.
- Migrations never run on normal startup; they run through the one-shot `--migrate` mode (the CD pipeline runs the same image with it).
- Never expose entities from controllers — DTOs only. Map in `MappingExtensions`.
- Validation lives in `Application/Validation` as `AbstractValidator<TDto>`; no DataAnnotations on DTOs. Max lengths must match the EF configuration.
- Services throw `NotFoundException` / `ConflictException` / `UnauthorizedException`; `GlobalExceptionHandler` maps them to 404/409/401 Problem Details.
- Repositories read with `AsNoTracking()`; services call `IUnitOfWork.SaveChangesAsync()` once per operation. Never `Include()` a navigation on an entity that is then passed to `Update()` (EF marks the whole graph Modified).
- Catalog writes use `[Authorize(Policy = Policies.AdminOnly)]`. Roles come from `Users.Role` (text). No endpoint grants Admin.
- Auth cookies are written/cleared only through `Controllers/AuthCookies.cs` (options must match for deletion to work).
- JWT uses short claim names (`sub`, `email`, `role`, `given_name`, `family_name`) with `MapInboundClaims = false`.
- State-changing endpoints are protected by `OriginValidationMiddleware`; a webhook that must accept cross-site posts uses `[SkipOriginValidation]` and verifies its own signature.
- Images: `ProductImages.Path` is a bucket object path or absolute https URL; `IImageUrlResolver` produces the public URL. Lowest `SortOrder` = primary image.
- Prices: `numeric(10,2)`, USD only. Server always recalculates totals — never trust client prices.
- Package versions: shared `Microsoft.Extensions.*` floors are floating (`[10.0.12,)`) to avoid NU1605 downgrade errors. `System.Security.Cryptography.Xml`/`Pkcs` are pinned to `[10.0.13,)` for security fixes.
- Every new business rule or bug fix comes with a unit test. Tests are named as sentences (`Reusing_an_already_rotated_token_revokes_every_session_of_the_user`) and pass `TestHelpers.Ct` to async calls.
- Do not add VSTest-only packages (`Microsoft.NET.Test.Sdk`, `xunit.runner.visualstudio`, `coverlet.collector`) — VSTest is not supported for xUnit v3 on the .NET 10 SDK.
- CI (`.github/workflows/ci.yml`) must stay green: build, tests, and the NuGet vulnerability audit (fails on High/Critical). XML comments in `.csproj` files must not contain `--`.
- Dockerfile, `.dockerignore` and YAML files keep LF line endings (`.gitattributes`).

## Git flow
One feature branch per topic → pull request into `developer` (CI must pass) → `developer` into `main` when stable → `main` deploys. The repository owner makes all commits and pushes: propose a commit message (Conventional Commits, English), never commit or push on your own.
