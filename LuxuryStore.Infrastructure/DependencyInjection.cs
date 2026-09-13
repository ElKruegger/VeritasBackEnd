using LuxuryStore.Application.Interfaces;
using LuxuryStore.Infrastructure.Persistence;
using LuxuryStore.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LuxuryStore.Infrastructure;

/// <summary>
/// Everything the Infrastructure layer needs to register lives behind this
/// one extension method. Program.cs calls
/// builder.Services.AddInfrastructure(builder.Configuration) without
/// knowing — or needing to know — that Postgres, EF Core or these
/// repository classes exist; it only ever depends on the Application
/// layer's interfaces (IUnitOfWork, IProductRepository...). That's the
/// Dependency Inversion Principle in practice: high-level code (Program.cs,
/// the Application services) depends on abstractions, and this is the one
/// place that wires the concrete Infrastructure types behind them.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' was not found. Set it via user-secrets locally " +
                "(dotnet user-secrets set \"ConnectionStrings:DefaultConnection\" \"...\") or the " +
                "ConnectionStrings__DefaultConnection environment variable in every other environment.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        // Scoped: one instance per HTTP request, matching AppDbContext's own
        // lifetime (AddDbContext registers it as Scoped by default).
        //   - Never Singleton: DbContext is not thread-safe, so a Singleton
        //     UnitOfWork would hand every concurrent request the same
        //     DbContext instance and corrupt data under load.
        //   - Never Transient here: each repository inside the same
        //     UnitOfWork needs to share one DbContext, and each service
        //     resolved during one request needs to share one UnitOfWork —
        //     Transient would break both and Brands/Categories/Products
        //     would stop sharing a transaction.
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
