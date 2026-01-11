using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Http;

namespace InventoryManagementSystem.Filters
{
    // Allows access only when session role is exactly "Admin"; otherwise redirect to user dashboard
    public class RequireAdminRoleAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var httpContext = context.HttpContext;
            var role = httpContext.Session.GetString("UserRole") ?? "User";
            if (role.ToLower() != "admin")
            {
                context.Result = new RedirectToActionResult("UserDashboard", "Dashboard", null);
                return;
            }
            base.OnActionExecuting(context);
        }
    }
}
