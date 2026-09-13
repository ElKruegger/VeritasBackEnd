using LuxuryStore.Application;
using LuxuryStore.Infrastructure;
using LuxuryStore_Backend.Middleware;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

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

// Centralized error handling — see Middleware/GlobalExceptionHandler.cs.
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseExceptionHandler();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(); // Test UI at /scalar — replaces the old Swagger UI in .NET 10.
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/health"); // Render pings this per brief section 3/10.

app.Run();
