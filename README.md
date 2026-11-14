# Véritas — Backend API

[![CI](https://github.com/ElKruegger/VeritasBackEnd/actions/workflows/ci.yml/badge.svg?branch=developer)](https://github.com/ElKruegger/VeritasBackEnd/actions/workflows/ci.yml)

REST API for **Véritas**, a luxury menswear and lifestyle e-commerce platform (tailoring, footwear, watches, accessories and fragrances).

Built with ASP.NET Core on .NET 10 following a layered Clean Architecture, backed by PostgreSQL on Supabase.

## Tech stack

| Concern | Choice |
|---|---|
| Runtime / framework | .NET 10, ASP.NET Core Web API (controllers) |
| Persistence | PostgreSQL (Supabase), Entity Framework Core 10 + Npgsql |
| Authentication | Custom JWT (HS256) in httpOnly cookies, refresh-token rotation with reuse detection |
| Authorization | Role-based policies (`Customer`, `Admin`) |
| Validation | FluentValidation, executed by a global action filter |
| Logging | Serilog (structured, console sink) |
| API documentation | OpenAPI document + Scalar UI (development only) |
| Images | Supabase Storage (public bucket), metadata in `ProductImages` |
| Testing | xUnit v3 on Microsoft.Testing.Platform, NSubstitute, Shouldly |
| CI | GitHub Actions, Dependabot |

## Solution layout

```
LuxuryStore-Backend/            (repository root)
├── LuxuryStore.Domain          Entities and enums. No dependencies.
├── LuxuryStore.Application     DTOs, service and repository interfaces, services,
│                               mapping, FluentValidation validators.
├── LuxuryStore.Infrastructure  EF Core DbContext, configurations, migrations,
│                               repositories, JWT issuing, storage URL resolver,
│                               catalog seeder.
├── LuxuryStore-Backend         API host: controllers, Program.cs, middleware,
│                               filters, authorization policies.
└── LuxuryStore.UnitTests       Unit tests for the Application and
                                Infrastructure layers.
```

Dependencies point inwards only: API → Application/Infrastructure → Domain. Application never references Infrastructure.

## Getting started

### Prerequisites

- .NET 10 SDK
- `dotnet-ef` CLI: `dotnet tool install --global dotnet-ef`
- A PostgreSQL database (a Supabase project, using the **Session Pooler** connection string)
- A trusted local HTTPS certificate: `dotnet dev-certs https --trust` (auth cookies are `Secure`)

### Local secrets

Secrets are never committed. Set them with user-secrets from the `LuxuryStore-Backend` project folder:

```bash
cd LuxuryStore-Backend
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<supabase session pooler connection string>"
dotnet user-secrets set "Jwt:SigningKey" "<random value, at least 32 bytes>"
```

A suitable signing key can be generated in PowerShell with:

```powershell
[Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(48))
```

### Database

Run from the repository root:

```bash
# Inspect which migrations are applied ("(Pending)" marks the ones that are not)
dotnet ef migrations list --project LuxuryStore.Infrastructure --startup-project LuxuryStore-Backend

# Apply all pending migrations
dotnet ef database update --project LuxuryStore.Infrastructure --startup-project LuxuryStore-Backend

# Load the demo catalog (idempotent: existing rows are never modified)
dotnet run --project LuxuryStore-Backend -- --seed
```

### Run

```bash
dotnet run --project LuxuryStore-Backend --launch-profile https
```

- API: `https://localhost:7214/api/v1`
- Interactive documentation (development only): `https://localhost:7214/scalar`
- Health check: `https://localhost:7214/health`

## Testing

```bash
dotnet test
```

`global.json` opts `dotnet test` into Microsoft.Testing.Platform, which xUnit v3 requires on the .NET 10 SDK.

The unit test suite runs without a database or network and covers:

- **Validation rules**: slugs, SKUs, prices, password policy, image paths (including path traversal attempts) and catalog query parameters.
- **Authentication**: email normalization, password hashing, generic login errors, deactivated accounts, refresh-token rotation, reuse detection that revokes every session, and idempotent logout.
- **Catalog services**: duplicate slugs, missing brands, image ordering and ownership checks.
- **Mapping**: effective variant price, gallery order and primary image selection.
- **Security primitives**: JWT claims, algorithm and lifetime, minimum signing-key length, refresh-token hashing.
- **Image URL resolution**: bucket URLs, escaping and placeholders.

## Continuous integration

Every pull request into `developer` or `main`, and every push to those branches, runs the [CI workflow](.github/workflows/ci.yml):

1. Restore and build the solution in Release configuration.
2. Run the unit test suite.
3. Audit direct and transitive NuGet packages; the run fails on any High or Critical vulnerability.

Dependabot opens weekly pull requests for NuGet packages and GitHub Actions, and each of them goes through the same pipeline.

## Continuous deployment

When CI finishes green on a push to `main`, the [CD workflow](.github/workflows/cd.yml) runs:

1. **Build** the Docker image once and push it to GitHub Container Registry as `ghcr.io/<owner>/veritas-api:sha-<commit>` (plus `latest`).
2. **Migrate**: run that same image with `--migrate` against the production database. A failed migration exits non-zero and stops the pipeline.
3. **Deploy**: call the Render deploy hook with `imgURL` set to that same image tag.

The schema and the running code therefore always come from the same build. Pull requests never deploy.

### One-time setup

**GitHub** (Settings → Environments → `production`), environment secrets:

| Secret | Value |
|---|---|
| `SUPABASE_CONNECTION_STRING` | Npgsql connection string for the Supabase **session pooler** (`Host=aws-0-<region>.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.<project-ref>;Password=...;SSL Mode=Require;Gss Encryption Mode=Disable`). The direct `db.<ref>.supabase.co` host is IPv6-only and unreachable from GitHub-hosted runners. |
| `RENDER_DEPLOY_HOOK_URL` | Render service → Settings → Deploy Hook |

Required reviewers can be added to the environment to make every production deploy wait for a manual approval.

**GHCR**: after the first run, the `veritas-api` package appears under the owner's Packages. Either make it public, or add a registry credential in Render (GitHub username + a personal access token with `read:packages`).

**Render**: create a *Web Service* from an *existing image* (`ghcr.io/<owner>/veritas-api:latest`), turn auto-deploy off (the pipeline deploys), and set:

| Setting | Value |
|---|---|
| Health check path | `/health` |
| `PORT` | `8080` |
| `ConnectionStrings__DefaultConnection` | Same session-pooler connection string as above (the transaction pooler on port 6543 would require disabling Npgsql's prepared statements) |
| `Jwt__SigningKey` | Random value, at least 32 bytes |
| `FRONTEND_URL` | Deployed frontend origin(s), comma-separated |
| `Storage__PublicBaseUrl` | Public URL of the `product-images` bucket |

### Running it by hand

```
docker build -t veritas-api .
docker run --rm -e ConnectionStrings__DefaultConnection="..." veritas-api --migrate
```

## Configuration

| Key | Environment variable | Purpose |
|---|---|---|
| `ConnectionStrings:DefaultConnection` | `ConnectionStrings__DefaultConnection` | PostgreSQL connection string (secret) |
| `Jwt:SigningKey` | `Jwt__SigningKey` | HS256 signing key, minimum 32 bytes (secret) |
| `Jwt:Issuer`, `Jwt:Audience` | `Jwt__Issuer`, `Jwt__Audience` | Token issuer and audience |
| `Jwt:AccessTokenMinutes`, `Jwt:RefreshTokenDays` | — | Token lifetimes (default 15 minutes / 14 days) |
| `Cors:AllowedOrigins` | `FRONTEND_URL` (comma-separated) | Origins allowed to call the API with credentials |
| `Storage:PublicBaseUrl` | `Storage__PublicBaseUrl` | Public URL of the `product-images` bucket |

When `Storage:PublicBaseUrl` is empty, product images resolve to neutral placeholders that display each image's alt text, so the catalog renders before real photography is uploaded.

## API overview

All routes are versioned under `/api/v1`.

| Area | Endpoints | Access |
|---|---|---|
| Auth | `POST /auth/register`, `POST /auth/login`, `POST /auth/refresh`, `POST /auth/logout`, `GET /auth/me` | Public (rate-limited) / authenticated for `me` |
| Brands | `GET /brands`, `GET /brands/{id}` · `POST`, `PUT`, `DELETE` | Reads public · writes `Admin` |
| Categories | `GET /categories`, `GET /categories/{id}` · `POST`, `PUT`, `DELETE` | Reads public · writes `Admin` |
| Products | `GET /products` (filters: `brandId`, `categoryId`, `minPrice`, `maxPrice`, `search`, paging), `GET /products/{id}`, `GET /products/slug/{slug}` · `POST`, `PUT`, `DELETE` | Reads public · writes `Admin` |
| Variants | `POST /products/{id}/variants`, `PUT`/`DELETE /products/{id}/variants/{variantId}` | `Admin` |
| Images | `POST /products/{id}/images`, `DELETE /products/{id}/images/{imageId}` | `Admin` |

Errors use RFC 9457 Problem Details. Validation failures return `400` with an `errors` dictionary keyed by property name.

## Security notes

- Access and refresh tokens are delivered only as `httpOnly`, `Secure`, `SameSite=None` cookies; they never appear in a response body.
- Refresh tokens are stored as SHA-256 hashes, rotated on every use, and reuse of a rotated token revokes every session of the account.
- Authentication endpoints are rate-limited per client IP.
- Row Level Security is enabled on every table so that Supabase's auto-generated Data API cannot read or write application data. Every migration that creates a table must also enable RLS on it.
- CORS is restricted to the configured frontend origins.
- After 5 consecutive wrong passwords an account is locked for 15 minutes. The lock is temporary on purpose, so nobody can lock a customer out permanently.
- Every failed login (unknown email, wrong password, locked account) returns the same message and takes the same time (a dummy password hash is verified for unknown emails), so the login form cannot be used to discover registered emails. A deactivated account is only reported after the correct password. Registration still answers 409 for a taken email until email confirmation exists.
- Every response carries `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, a `Content-Security-Policy` that forbids loading anything and being framed, `Referrer-Policy: no-referrer` and a restrictive `Permissions-Policy`; the `Server` header is removed. Outside Development, HTTPS responses also carry `Strict-Transport-Security` (1 year).
- Expired refresh tokens are deleted by a background job every 6 hours.

### Granting admin access

There is intentionally no API endpoint that grants elevated privileges. An operator promotes an existing account directly in the database (Supabase SQL editor):

```sql
UPDATE "Users" SET "Role" = 'Admin' WHERE "Email" = 'operator@example.com';
```

The new role takes effect at the account's next login or token refresh.

## Product images

Images are stored in a public Supabase Storage bucket named `product-images`. Each `ProductImages` row holds the object path inside that bucket (for example `products/rolex-submariner-date/01.jpg`), alternative text and a sort order; the image with the lowest sort order is the product's primary image. The seeded catalog expects three images per product, named `01.jpg`, `02.jpg` and `03.jpg` under `products/<product-slug>/`.
