using System.Text;
using LuxuryStore.Application;
using LuxuryStore.Infrastructure;
using LuxuryStore_Backend.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
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
    builder.Services.AddControllers();

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

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtSection["Issuer"],
                ValidateAudience = true,
                ValidAudience = jwtSection["Audience"],
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30) // access tokens are short-lived on purpose; the 5-minute default is too generous here
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
    builder.Services.AddAuthorization();

    // Rate limiting (brief section 9): fixed-window, per-client-IP. Applied
    // to Register/Login/Refresh via [EnableRateLimiting("auth")] — exactly
    // the endpoints an unauthenticated attacker would hammer. Checkout gets
    // its own policy once that endpoint exists (brief 7.4).
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.AddFixedWindowLimiter("auth", limiterOptions =>
        {
            limiterOptions.Window = TimeSpan.FromMinutes(1);
            limiterOptions.PermitLimit = 5;
            limiterOptions.QueueLimit = 0;
        });
    });

    // Centralized error handling — see Middleware/GlobalExceptionHandler.cs.
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails();

    builder.Services.AddHealthChecks();

    var app = builder.Build();

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

    app.UseRateLimiter();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    app.MapHealthChecks("/health"); // Render pings this per brief section 3/10.

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
