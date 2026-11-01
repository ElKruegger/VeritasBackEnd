using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace LuxuryStore_Backend.Filters;

/// <summary>
/// Runs the FluentValidation validator (LuxuryStore.Application/Validation)
/// for every action argument that has one, before the action executes —
/// brief section 9: "server-side input validation on every endpoint".
///
/// Registered globally in Program.cs (AddControllers(o => o.Filters.Add...)),
/// so a new endpoint is validated as soon as a validator exists for its DTO,
/// with no attribute or manual ValidateAsync call to forget.
///
/// Failures are returned in exactly the same shape [ApiController] already
/// uses for model-binding errors (RFC 9457 ValidationProblemDetails, built by
/// ProblemDetailsFactory), so the frontend handles one 400 format only.
///
/// Why not FluentValidation.AspNetCore's auto-validation: that package is
/// deprecated by its authors (synchronous MVC pipeline integration, no async
/// rules). An explicit action filter is the approach they recommend instead.
/// </summary>
public class FluentValidationFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var services = context.HttpContext.RequestServices;

        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null)
                continue;

            var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
            if (services.GetService(validatorType) is not IValidator validator)
                continue;

            var result = await validator.ValidateAsync(
                new ValidationContext<object>(argument), context.HttpContext.RequestAborted);

            foreach (var error in result.Errors)
                context.ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
        }

        if (!context.ModelState.IsValid)
        {
            var problem = services.GetRequiredService<ProblemDetailsFactory>()
                .CreateValidationProblemDetails(context.HttpContext, context.ModelState);

            context.Result = new ObjectResult(problem) { StatusCode = problem.Status };
            return;
        }

        await next();
    }
}
