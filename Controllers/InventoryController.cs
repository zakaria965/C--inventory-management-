using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InventoryManagementSystem.Data;
using InventoryManagementSystem.Models;

namespace InventoryManagementSystem.Controllers
{
    public class InventoryController : Controller
    {
        private readonly ApplicationDbContext _context;

        public InventoryController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Inventory
        public async Task<IActionResult> Index(string searchString, string categoryFilter)
        {
            var products = from p in _context.Products select p;

            if (!string.IsNullOrEmpty(searchString))
            {
                products = products.Where(p => p.Name.Contains(searchString) 
                    || p.SKU.Contains(searchString) 
                    || (p.Description != null && p.Description.Contains(searchString)));
            }

            if (!string.IsNullOrEmpty(categoryFilter))
            {
                products = products.Where(p => p.Category == categoryFilter);
            }

            var categories = await _context.Products
                .Where(p => p.Category != null)
                .Select(p => p.Category)
                .Distinct()
                .ToListAsync();

            ViewBag.Categories = categories;
            ViewBag.SearchString = searchString;
            ViewBag.CategoryFilter = categoryFilter;

            // Check for low stock items
            var lowStockProducts = await products
                .Where(p => p.MinimumStockLevel.HasValue && p.QuantityInStock <= p.MinimumStockLevel.Value)
                .ToListAsync();

            ViewBag.LowStockProducts = lowStockProducts;

            return View(await products.OrderBy(p => p.Name).ToListAsync());
        }

        // GET: Inventory/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .Include(p => p.Outgoings)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }
    }
}
