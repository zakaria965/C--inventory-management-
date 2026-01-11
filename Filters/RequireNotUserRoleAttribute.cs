using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Http;

namespace InventoryManagementSystem.Filters
{
    // Blocks access when session role is exactly "User"; redirect to user dashboard
    public class RequireNotUserRoleAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var httpContext = context.HttpContext;
            var role = httpContext.Session.GetString("UserRole") ?? "User";
            if (role.ToLower() == "user")
            {
                context.Result = new RedirectToActionResult("User", "Dashboard", null);
                return;
            }
            base.OnActionExecuting(context);
        }
    }
}
