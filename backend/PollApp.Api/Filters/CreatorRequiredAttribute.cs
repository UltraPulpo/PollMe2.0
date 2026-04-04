using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace PollApp.Api.Filters;

/// <summary>
/// Action filter that requires a valid creator identity.
/// If no creator can be resolved from cookie or route, returns 401.
/// On success, stores the Creator in HttpContext.Items["Creator"].
/// </summary>
public class CreatorRequiredAttribute : ActionFilterAttribute
{
    public override async Task OnActionExecutionAsync(
        ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var authService = context.HttpContext.RequestServices
            .GetRequiredService<Services.ICreatorAuthService>();
        var creator = await authService.GetCurrentCreatorAsync(context.HttpContext);

        if (creator == null)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        // Store creator in HttpContext.Items so the controller can access it
        context.HttpContext.Items["Creator"] = creator;
        await next();
    }
}
