using System.Globalization;
using FluentValidation;
using LuxuryStore.Application.Interfaces;
using LuxuryStore.Application.Services;
using LuxuryStore.Application.Validation;
using LuxuryStore.Domain.Entities;
using Microsoft.AspNetCore.Identity;
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
        services.AddScoped<IAuthService, AuthService>();

        // ASP.NET Core Identity's password hasher used standalone — PBKDF2
        // under the hood, salted per-password automatically — without
        // pulling in the rest of Identity (UserManager/SignInManager/
        // IdentityDbContext), which would mean rebuilding this project's
        // existing IUnitOfWork/repository pattern around Identity's own
        // store abstractions instead. Stateless and thread-safe, so
        // Singleton is correct (and avoids reallocating it every request).
        services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();

        // Every AbstractValidator<T> in this assembly (Validation/), resolved
        // per request by the API's FluentValidationFilter. Scoped is the
        // library default and matches the services above.
        services.AddValidatorsFromAssemblyContaining<RegisterDtoValidator>();

        // FluentValidation localizes its built-in messages from the OS UI
        // culture (a pt-BR Windows answered "'Email' é um endereço de email
        // inválido."). The API contract is English-only (brief section 1), so
        // the default messages are pinned to English regardless of the machine.
        ValidatorOptions.Global.LanguageManager.Culture = new CultureInfo("en");

        return services;
    }
}
