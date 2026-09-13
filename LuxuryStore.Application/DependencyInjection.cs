using LuxuryStore.Application.Interfaces;
using LuxuryStore.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace LuxuryStore.Application;

/// <summary>
/// Registers everything this layer owns behind one extension method, so
/// Program.cs calls builder.Services.AddApplication() without needing to
/// list every service by hand.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Scoped: a new instance per HTTP request. That matches IUnitOfWork's
        // own lifetime (also Scoped — see LuxuryStore.Infrastructure), so a
        // service and everything it calls during one request share the same
        // DbContext/transaction. Transient would still work correctly here
        // (these services hold no mutable state of their own) but would be
        // wasteful to allocate repeatedly within a single request; Singleton
        // would be wrong the moment a service ever depended on something
        // request-scoped, so Scoped is the safe default for anything that,
        // even indirectly, touches IUnitOfWork.
        services.AddScoped<IBrandService, BrandService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IProductService, ProductService>();

        return services;
    }
}
