using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AutoTechHub.Filters
{
    public class EnforcePasswordChangeFilter : IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var user = context.HttpContext.User;
            if (user != null && user.Identity != null && user.Identity.IsAuthenticated)
            {
                var mustChange = user.FindFirst("MustChangePassword")?.Value;
                if (string.Equals(mustChange, "true", StringComparison.OrdinalIgnoreCase))
                {
                    var controller = context.RouteData.Values["controller"]?.ToString() ?? string.Empty;
                    var action = context.RouteData.Values["action"]?.ToString() ?? string.Empty;

                    bool isAllowedEndpoint =
                        controller.Equals("Media", StringComparison.OrdinalIgnoreCase) ||
                        (controller.Equals("Account", StringComparison.OrdinalIgnoreCase) &&
                        (action.Equals("ForceChangePassword", StringComparison.OrdinalIgnoreCase) ||
                         action.Equals("Logout", StringComparison.OrdinalIgnoreCase) ||
                         action.Equals("AccessDenied", StringComparison.OrdinalIgnoreCase)));

                    if (!isAllowedEndpoint)
                    {
                        context.Result = new RedirectToActionResult("ForceChangePassword", "Account", null);
                        return;
                    }
                }
            }

            await next();
        }
    }
}
