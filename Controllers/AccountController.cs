using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using InventoryManagementSystem.Data;
using System.Linq;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;

namespace InventoryManagementSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AccountController(ApplicationDbContext context, UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager, RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
        }

        // GET: Account/Login
        public IActionResult Login()
        {
            if (User?.Identity?.IsAuthenticated == true)
            {
                // Redirect based on role claim
                if (User.IsInRole("Admin")) return RedirectToAction("Index", "Dashboard");
                return RedirectToAction("UserDashboard", "Dashboard");
            }
            return View();
        }

        // POST: Account/Login
        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                TempData["Error"] = "Email and password are required.";
                return View();
            }

            // Ensure any previous authentication is cleared to avoid role caching
            await _signInManager.SignOutAsync();

            var result = await _signInManager.PasswordSignInAsync(email, password, isPersistent: false, lockoutOnFailure: false);
            if (result.Succeeded)
            {
                var user = await _userManager.FindByEmailAsync(email);
                var roles = user == null ? new System.Collections.Generic.List<string>() : (await _userManager.GetRolesAsync(user)).ToList();

                // Ensure single role - if multiple, pick first (you may enforce stricter rules elsewhere)
                var role = roles.FirstOrDefault() ?? "User";

                // Refresh session info
                HttpContext.Session.SetString("IsLoggedIn", "true");
                HttpContext.Session.SetString("UserEmail", email);
                HttpContext.Session.SetString("UserName", user?.UserName ?? email.Split('@')[0]);
                HttpContext.Session.SetString("UserRole", role);

                TempData["Success"] = "Login successful!";
                if (string.Equals(role, "Admin", System.StringComparison.OrdinalIgnoreCase))
                    return RedirectToAction("Index", "Dashboard");
                return RedirectToAction("UserDashboard", "Dashboard");
            }

            TempData["Error"] = "Invalid email or password.";
            return View();
        }

        // GET: Account/SignUp
        public IActionResult SignUp()
        {
            if (User?.Identity?.IsAuthenticated == true)
            {
                if (User.IsInRole("Admin")) return RedirectToAction("Index", "Dashboard");
                return RedirectToAction("UserDashboard", "Dashboard");
            }
            return View();
        }

        // POST: Account/SignUp
        [HttpPost]
        public async Task<IActionResult> SignUp(string name, string email, string password, string confirmPassword, string role)
        {
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(email) || 
                string.IsNullOrEmpty(password) || string.IsNullOrEmpty(confirmPassword))
            {
                TempData["Error"] = "All fields are required.";
                return View();
            }

            if (password != confirmPassword)
            {
                TempData["Error"] = "Passwords do not match.";
                return View();
            }

            if (password.Length < 6)
            {
                TempData["Error"] = "Password must be at least 6 characters long.";
                return View();
            }

            // Decide role
            var normalizedRole = string.IsNullOrWhiteSpace(role) ? "User" : role;

            // Create Identity user
            var user = new IdentityUser { UserName = email, Email = email };
            var createResult = await _userManager.CreateAsync(user, password);
            if (!createResult.Succeeded)
            {
                TempData["Error"] = string.Join("; ", createResult.Errors.Select(e => e.Description));
                return View();
            }

            // Ensure roles exist
            if (!await _roleManager.RoleExistsAsync("Admin")) await _roleManager.CreateAsync(new IdentityRole("Admin"));
            if (!await _roleManager.RoleExistsAsync("User")) await _roleManager.CreateAsync(new IdentityRole("User"));

            if (normalizedRole.Equals("admin", System.StringComparison.OrdinalIgnoreCase))
            {
                await _userManager.AddToRoleAsync(user, "Admin");

                // also keep a TeamProfile for admin metadata
                var existing = _context.TeamProfiles.FirstOrDefault(tp => tp.EmailAddress != null && tp.EmailAddress.ToLower() == email.ToLower());
                if (existing == null)
                {
                    var tp = new InventoryManagementSystem.Models.TeamProfile
                    {
                        FullName = name,
                        EmailAddress = email,
                        Role = "Admin",
                        Bio = "",
                        ProfileImagePath = null
                    };
                    _context.TeamProfiles.Add(tp);
                    _context.SaveChanges();
                }

                TempData["Success"] = "Admin account created. Please log in.";
                return RedirectToAction("Login");
            }

            // Normal user: assign User role and sign in
            await _userManager.AddToRoleAsync(user, "User");
            await _signInManager.SignInAsync(user, isPersistent: false);

            // Set session
            HttpContext.Session.SetString("IsLoggedIn", "true");
            HttpContext.Session.SetString("UserEmail", email);
            HttpContext.Session.SetString("UserName", name);
            HttpContext.Session.SetString("UserRole", "User");

            TempData["Success"] = "Account created and signed in.";
            return RedirectToAction("UserDashboard", "Dashboard");
        }

        // GET: Account/Logout - show confirmation page
        public IActionResult Logout()
        {
            return View();
        }

        // POST: Account/LogoutConfirmed - perform logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LogoutConfirmed()
        {
            // Sign out Identity cookies and clear session
            await _signInManager.SignOutAsync();
            HttpContext.Session.Clear();
            TempData["Success"] = "You have been logged out successfully.";
            // After logout show SignUp page per user request
            return RedirectToAction("SignUp");
        }
    }
}

