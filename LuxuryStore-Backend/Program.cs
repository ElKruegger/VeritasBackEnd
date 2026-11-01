using System.IdentityModel.Tokens.Jwt;
using System.Threading.RateLimiting;
using LuxuryStore.Application;
using LuxuryStore.Infrastructure;
using LuxuryStore.Infrastructure.Persistence.Seed;
using LuxuryStore.Infrastructure.Security;
using LuxuryStore_Backend.Authorization;
using LuxuryStore_Backend.Filters;
using LuxuryStore_Backend.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Serilog;

// Bootstrap logger: captures anything that goes wrong before the full
// Serilog pipeline (which needs configuration/DI) is up — startup errors
// would otherwise vanish silently.
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Reads the "Serilog" section from appsettings — see brief section 3
    // ("structured logging, excluding sensitive payment data from logs").
    // ReadFrom.Services lets sinks/enrichers registered in DI participate,
    // which nothing does yet but keeps this forward-compatible.
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    // Add services to the container.
    // FluentValidationFilter runs the Application layer's validators for
    // every action argument (brief section 9) — see Filters/FluentValidationFilter.cs.
    builder.Services.AddControllers(options => options.Filters.Add<FluentValidationFilter>());

    // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
    builder.Services.AddOpenApi();

    // Each layer registers its own services behind one extension method
    // (AddApplication / AddInfrastructure) — Program.cs stays a short,
    // readable list of "what" gets wired up without needing to know "how".
    // See LuxuryStore.Application/DependencyInjection.cs and
    // LuxuryStore.Infrastructure/DependencyInjection.cs for the actual
    // AddScoped calls and why each lifetime was chosen.
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    // --- Auth (brief 7.2) ---
    // JWT Bearer, but the token itself travels as an httpOnly cookie, never
    // read by frontend JS and never sent via an Authorization header — see
    // AuthController.SetAuthCookies. OnMessageReceived below is what lets
    // the same middleware validate a cookie-borne token; the Authorization
    // header path is left intact too, so Scalar/Postman-style manual testing
    // with a Bearer header still works during development.
    var jwtSection = builder.Configuration.GetSection("Jwt");
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            // IsNullOrWhiteSpace, not just "?? throw": appsettings.Development.json
            // ships an empty "" placeholder for this key (same pattern as
            // ConnectionStrings:DefaultConnection), and "" isn't null — a plain
            // "??" silently passes it through, and SymmetricSecurityKey then
            // throws its own much less helpful IDX10703 ("key length is zero")
            // instead of this message ever being seen.
            var signingKey = jwtSection["SigningKey"];
            if (string.IsNullOrWhiteSpace(signingKey))
                throw new InvalidOperationException(
                    "Configuration 'Jwt:SigningKey' was not found or is empty. Set it via user-secrets locally " +
                    "(dotnet user-secrets set \"Jwt:SigningKey\" \"...\" — at least 32 random characters — run " +
                    "from the LuxuryStore-Backend project folder, where the UserSecretsId lives) or the " +
                    "Jwt__SigningKey environment variable in every other environment.");

            // Keep the JWT's short claim names ("sub", "email", "role") as-is.
            // With the default (true), the handler silently renames them to
            // long WS-Fed URIs — "sub" becomes ClaimTypes.NameIdentifier — and
            // AuthController.Me's FindFirst("sub") would never match.
            options.MapInboundClaims = false;

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtSection["Issuer"],
                ValidateAudience = true,
                ValidAudience = jwtSection["Audience"],
                ValidateIssuerSigningKey = true,
                // Same key-building code (and >= 32-byte rule) as issuing.
                IssuerSigningKey = TokenService.CreateSigningKey(signingKey),
                // Pinned: only the algorithm this API actually signs with is
                // accepted — closes the door on algorithm-confusion tricks.
                ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30), // access tokens are short-lived on purpose; the 5-minute default is too generous here
                NameClaimType = JwtRegisteredClaimNames.Sub,
                RoleClaimType = JwtClaimTypes.Role // what RequireRole / the AdminOnly policy checks
            };

            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    if (string.IsNullOrEmpty(context.Token) &&
                        context.Request.Cookies.TryGetValue("access_token", out var cookieToken))
                    {
                        context.Token = cookieToken;
                    }
                    return Task.CompletedTask;
                }
            };
        });
    builder.Services.AddAuthorization(options =>
    {
        // Catalog writes (POST/PUT/DELETE on brands, categories, products,
        // variants, images). See Authorization/Policies.cs and UserRole for
        // how an account becomes Admin.
        options.AddPolicy(Policies.AdminOnly, policy => policy
            .RequireAuthenticatedUser()
            .RequireRole(nameof(LuxuryStore.Domain.Enums.UserRole.Admin)));
    });

    // --- CORS (brief section 9: "restricted to the deployed frontend origin only") ---
    // Origins come from two places, merged:
    //   - Cors:AllowedOrigins (array) in appsettings — dev defaults live in
    //     appsettings.Development.json (Vite on https://localhost:5173);
    //   - FRONTEND_URL (brief section 10), comma-separated, set on Render.
    // AllowCredentials is required for the httpOnly auth cookies to travel
    // cross-origin, and the CORS spec forbids combining it with a wildcard
    // origin — hence an explicit list, never AllowAnyOrigin.
    var allowedOrigins = (builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
        .Concat((builder.Configuration["FRONTEND_URL"] ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        .Select(origin => origin.TrimEnd('/')) // the browser's Origin header never has a trailing slash
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();

    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy => policy
            .WithOrigins(allowedOrigins)
            .AllowCredentials()
            .WithMethods("GET", "POST", "PUT", "DELETE")
            .WithHeaders("Content-Type", "Accept", "X-Requested-With", "Idempotency-Key")
            // Lets the browser cache the preflight for 10 minutes instead of
            // sending an OPTIONS before every single cross-origin call.
            .SetPreflightMaxAge(TimeSpan.FromMinutes(10)));
    });

    // Rate limiting (brief section 9): fixed window, partitioned per client
    // IP — each IP gets its own 5-requests/minute bucket. Applied to
    // Register/Login/Refresh via [EnableRateLimiting("auth")].
    //
    // The previous version used options.AddFixedWindowLimiter("auth", ...),
    // which has NO partition key: it was one single shared bucket for the
    // whole internet, so 5 login attempts from anyone locked everyone else
    // out for the rest of the minute. AddPolicy + RateLimitPartition is the
    // per-key form. Checkout gets its own policy once that endpoint exists
    // (brief 7.4), partitioned per user id for authenticated callers.
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.AddPolicy("auth", httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                // RemoteIpAddress is the real client IP only because
                // UseForwardedHeaders runs first in the pipeline (see below);
                // without it, every request on Render would share the
                // proxy's IP and we'd be back to one global bucket.
                partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    Window = TimeSpan.FromMinutes(1),
                    PermitLimit = 5,
                    QueueLimit = 0
                }));
    });

    // Render (and most PaaS hosts) terminate TLS at their edge proxy and
    // forward plain HTTP to the container, adding X-Forwarded-For (client
    // IP) and X-Forwarded-Proto (original scheme). Without honoring them,
    // RemoteIpAddress is the proxy's IP (breaks per-IP rate limiting) and
    // Request.Scheme is "http" (UseHttpsRedirection could redirect-loop).
    // The known-proxy lists are cleared because Render doesn't publish a
    // fixed proxy IP range — acceptable only because the container is not
    // reachable except through that proxy. ForwardLimit stays at the
    // default 1: only the hop added by the nearest proxy is trusted, so a
    // client can't spoof its IP by sending its own X-Forwarded-For.
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();
    });

    // Centralized error handling — see Middleware/GlobalExceptionHandler.cs.
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails();

    builder.Services.AddHealthChecks();

    var app = builder.Build();

    if (allowedOrigins.Length == 0)
        Log.Warning("CORS: no allowed origins configured (Cors:AllowedOrigins / FRONTEND_URL) — " +
                    "every cross-origin browser request will be rejected.");
    else
        Log.Information("CORS: allowed origins {Origins}", allowedOrigins);

    // One-shot maintenance mode: `dotnet run --project LuxuryStore-Backend -- --seed`
    // loads the demo catalog (Infrastructure/Persistence/Seed) and exits
    // without starting the web server. Never runs on a normal start.
    if (args.Contains("--seed"))
    {
        using var scope = app.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<CatalogSeeder>().SeedAsync();
        return;
    }

    // Must be first: everything below (rate limiter partitioning, HTTPS
    // redirection, request logging) reads the client IP / scheme.
    app.UseForwardedHeaders();

    app.UseExceptionHandler();

    // Structured per-request logs (method, path, status, elapsed) without a
    // bespoke logging middleware.
    app.UseSerilogRequestLogging();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference(); // Test UI at /scalar — replaces the old Swagger UI in .NET 10.
    }

    app.UseHttpsRedirection();

    // Before authentication/authorization, so a preflight (OPTIONS, which
    // never carries cookies) is answered here instead of being 401'd.
    app.UseCors();

    app.UseRateLimiter();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    app.MapHealthChecks("/health"); // Render pings this per brief section 3/10.

    app.Run();
}
// HostAbortedException is thrown on purpose by design-time tools (dotnet ef)
// after they grab the DbContext and stop the host — expected, not a crash.
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
