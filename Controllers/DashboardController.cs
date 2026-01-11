using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InventoryManagementSystem.Data;

namespace InventoryManagementSystem.Controllers
{
    using Microsoft.AspNetCore.Authorization;

    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            var dashboardStats = new
            {
                TotalProducts = await _context.Products.CountAsync(),
                TotalOrders = await _context.Orders.CountAsync(),
                TotalEmployees = await _context.Employees.CountAsync(),
                TotalSuppliers = await _context.Suppliers.CountAsync(),
                LowStockProducts = await _context.Products
                    .Where(p => p.MinimumStockLevel.HasValue && p.QuantityInStock <= p.MinimumStockLevel.Value)
                    .CountAsync(),
                TotalPurchases = await _context.Purchases.CountAsync(),
                TotalOutgoings = await _context.Outgoings.CountAsync(),
                PendingOrders = await _context.Orders.Where(o => o.Status == "Pending").CountAsync(),
                TotalInventoryValue = await _context.Products
                    .SumAsync(p => p.QuantityInStock * p.SellingPrice),
                TotalSalaryPaid = await _context.Salaries.Where(s => s.IsPaid).SumAsync(s => s.SalaryAmount),
                TotalSalaryUnpaid = await _context.Salaries.Where(s => !s.IsPaid).SumAsync(s => s.SalaryAmount),
                UnreadMessages = await _context.Messages.Where(m => !m.IsRead).CountAsync()
            };

            ViewBag.Stats = dashboardStats;

            // Recent orders
            var recentOrders = await _context.Orders
                .OrderByDescending(o => o.OrderDate)
                .Take(5)
                .ToListAsync();
            ViewBag.RecentOrders = recentOrders;

            // Low stock products
            var lowStockProducts = await _context.Products
                .Where(p => p.MinimumStockLevel.HasValue && p.QuantityInStock <= p.MinimumStockLevel.Value)
                .OrderBy(p => p.QuantityInStock)
                .Take(5)
                .ToListAsync();
            ViewBag.LowStockProducts = lowStockProducts;

            // Recent activities (combining purchases, outgoings, and orders)
            var recentPurchases = await _context.Purchases
                .Include(p => p.Product)
                .OrderByDescending(p => p.PurchaseDate)
                .Take(3)
                .Select(p => new { Type = "Purchase", Date = p.PurchaseDate, Description = $"Purchased {p.Quantity} {(p.Product != null ? p.Product.Name : "Unknown Product")}" })
                .ToListAsync();

            var recentOutgoings = await _context.Outgoings
                .Include(o => o.Product)
                .OrderByDescending(o => o.OutgoingDate)
                .Take(3)
                .Select(o => new { Type = "Outgoing", Date = o.OutgoingDate, Description = $"Outgoing {o.Quantity} {(o.Product != null ? o.Product.Name : "Unknown Product")} - {o.Reason}" })
                .ToListAsync();

            var recentOrdersList = await _context.Orders
                .OrderByDescending(o => o.OrderDate)
                .Take(3)
                .Select(o => new { Type = "Order", Date = o.OrderDate, Description = $"Order {o.OrderNumber} - {o.CustomerName}" })
                .ToListAsync();

            var allActivities = recentPurchases.Cast<object>()
                .Concat(recentOutgoings.Cast<object>())
                .Concat(recentOrdersList.Cast<object>())
                .OrderByDescending(a => ((dynamic)a).Date)
                .Take(10)
                .ToList();

            ViewBag.RecentActivities = allActivities;

            return View();
        }

        [Authorize(Roles = "User")]
        public async Task<IActionResult> UserDashboard()
        {
            // Use the authenticated user's email from claims
            var userEmail = User?.Identity?.Name;
            if (string.IsNullOrEmpty(userEmail))
            {
                return RedirectToAction("Login", "Account");
            }

            var userOrders = await _context.Orders
                .Where(o => o.CustomerEmail == userEmail)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(userOrders);
        }
    }
}
