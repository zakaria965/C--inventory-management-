using Microsoft.AspNetCore.Mvc;

namespace InventoryManagementSystem.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            // If not logged in, redirect to login page
            if (HttpContext.Session.GetString("IsLoggedIn") != "true")
            {
                return RedirectToAction("Login", "Account");
            }
            // If logged in, redirect to dashboard
            return RedirectToAction("Index", "Dashboard");
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}

