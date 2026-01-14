using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InventoryManagementSystem.Data;
using InventoryManagementSystem.Filters;
using InventoryManagementSystem.Models;

namespace InventoryManagementSystem.Controllers
{
    [RequireAdminRole]
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Product/Add
        public async Task<IActionResult> Add()
        {
            // Load active suppliers for dropdown
            var suppliers = await _context.Suppliers
                .Where(s => s.IsActive)
                .OrderBy(s => s.Name)
                .ToListAsync();
            ViewBag.Suppliers = suppliers;
            // Load categories for dropdown (from Categories table if available)
            if (await _context.Categories.AnyAsync())
            {
                ViewBag.Categories = await _context.Categories.OrderBy(c => c.Name).Select(c => c.Name).ToListAsync();
            }
            else
            {
                ViewBag.Categories = new List<string> { "Furniture", "Electronics", "Building Materials" };
            }
            return View();
        }

        // POST: Product/Add
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(Product product)
        {
            // Check for duplicate SKU
            if (await _context.Products.AnyAsync(p => p.SKU == product.SKU))
            {
                ModelState.AddModelError("SKU", "A product with this SKU already exists.");
            }

            // Auto-set supplier based on category if not provided
            if (string.IsNullOrWhiteSpace(product.Supplier) && !string.IsNullOrWhiteSpace(product.Category))
            {
                var supplier = await _context.Suppliers
                    .Where(s => s.IsActive && s.Category == product.Category)
                    .FirstOrDefaultAsync();
                
                if (supplier != null)
                {
                    product.Supplier = supplier.Name;
                }
            }

            // Set UnitPrice to SellingPrice for backward compatibility
            if (product.UnitPrice == 0 && product.SellingPrice > 0)
            {
                product.UnitPrice = product.SellingPrice;
            }

            if (ModelState.IsValid)
            {
                product.CreatedDate = DateTime.Now;
                _context.Products.Add(product);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Product added successfully!";
                return RedirectToAction("Index", "Inventory");
            }

            // Reload suppliers for dropdown
            var suppliers = await _context.Suppliers
                .Where(s => s.IsActive)
                .OrderBy(s => s.Name)
                .ToListAsync();
            ViewBag.Suppliers = suppliers;
            // Reload categories for dropdown
            if (await _context.Categories.AnyAsync())
            {
                ViewBag.Categories = await _context.Categories.OrderBy(c => c.Name).Select(c => c.Name).ToListAsync();
            }
            else
            {
                ViewBag.Categories = new List<string> { "Furniture", "Electronics", "Building Materials" };
            }
            return View(product);
        }

        // GET: Product/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            // Load active suppliers for dropdown
            var suppliers = await _context.Suppliers
                .Where(s => s.IsActive)
                .OrderBy(s => s.Name)
                .ToListAsync();
            ViewBag.Suppliers = suppliers;
            // Load categories for dropdown
            if (await _context.Categories.AnyAsync())
            {
                ViewBag.Categories = await _context.Categories.OrderBy(c => c.Name).Select(c => c.Name).ToListAsync();
            }
            else
            {
                ViewBag.Categories = new List<string> { "Furniture", "Electronics", "Building Materials" };
            }
            return View(product);
        }

        // POST: Product/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Product product)
        {
            if (id != product.Id)
            {
                return NotFound();
            }

            // Auto-set supplier based on category if not provided
            if (string.IsNullOrWhiteSpace(product.Supplier) && !string.IsNullOrWhiteSpace(product.Category))
            {
                var supplier = await _context.Suppliers
                    .Where(s => s.IsActive && s.Category == product.Category)
                    .FirstOrDefaultAsync();
                
                if (supplier != null)
                {
                    product.Supplier = supplier.Name;
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    product.LastUpdated = DateTime.Now;
                    _context.Update(product);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Product updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductExists(product.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction("Index", "Inventory");
            }

            // Reload suppliers for dropdown
            var suppliers = await _context.Suppliers
                .Where(s => s.IsActive)
                .OrderBy(s => s.Name)
                .ToListAsync();
            ViewBag.Suppliers = suppliers;
            // Reload categories for dropdown
            if (await _context.Categories.AnyAsync())
            {
                ViewBag.Categories = await _context.Categories.OrderBy(c => c.Name).Select(c => c.Name).ToListAsync();
            }
            else
            {
                ViewBag.Categories = new List<string> { "Furniture", "Electronics", "Building Materials" };
            }
            return View(product);
        }

        // GET: Product/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .FirstOrDefaultAsync(m => m.Id == id);
            if (product == null)
            {
                return NotFound();
            }

            // Check for related records to show warning
            var hasOrderItems = await _context.OrderItems.AnyAsync(oi => oi.ProductId == id);
            var hasOutgoings = await _context.Outgoings.AnyAsync(o => o.ProductId == id);
            var hasPurchases = await _context.Purchases.AnyAsync(p => p.ProductId == id);

            if (hasOrderItems || hasOutgoings || hasPurchases)
            {
                var relatedCounts = new
                {
                    OrderItems = hasOrderItems ? await _context.OrderItems.CountAsync(oi => oi.ProductId == id) : 0,
                    Outgoings = hasOutgoings ? await _context.Outgoings.CountAsync(o => o.ProductId == id) : 0,
                    Purchases = hasPurchases ? await _context.Purchases.CountAsync(p => p.ProductId == id) : 0
                };
                ViewBag.RelatedRecords = relatedCounts;
                ViewBag.HasRelatedRecords = true;
            }
            else
            {
                ViewBag.HasRelatedRecords = false;
            }

            return View(product);
        }

        // POST: Product/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products
                .Include(p => p.OrderItems)
                .Include(p => p.Outgoings)
                .FirstOrDefaultAsync(p => p.Id == id);
            
            if (product == null)
            {
                TempData["Error"] = "Product not found.";
                return RedirectToAction("Index", "Inventory");
            }

            // Check for related records that prevent deletion
            var hasOrderItems = await _context.OrderItems.AnyAsync(oi => oi.ProductId == id);
            var hasOutgoings = await _context.Outgoings.AnyAsync(o => o.ProductId == id);
            var hasPurchases = await _context.Purchases.AnyAsync(p => p.ProductId == id);

            if (hasOrderItems || hasOutgoings || hasPurchases)
            {
                var relatedRecords = new List<string>();
                if (hasOrderItems)
                {
                    var orderItemCount = await _context.OrderItems.CountAsync(oi => oi.ProductId == id);
                    relatedRecords.Add($"{orderItemCount} order item(s)");
                }
                if (hasOutgoings)
                {
                    var outgoingCount = await _context.Outgoings.CountAsync(o => o.ProductId == id);
                    relatedRecords.Add($"{outgoingCount} outgoing record(s)");
                }
                if (hasPurchases)
                {
                    var purchaseCount = await _context.Purchases.CountAsync(p => p.ProductId == id);
                    relatedRecords.Add($"{purchaseCount} purchase record(s)");
                }

                TempData["Error"] = $"Cannot delete product '{product.Name}' because it has related records: {string.Join(", ", relatedRecords)}. Please remove or update these records first.";
                return RedirectToAction("Index", "Inventory");
            }

            // No related records, safe to delete
            try
            {
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Product deleted successfully!";
            }
            catch (DbUpdateException ex)
            {
                TempData["Error"] = $"Error deleting product: {ex.InnerException?.Message ?? ex.Message}. The product may have related records that prevent deletion.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"An error occurred while deleting the product: {ex.Message}";
            }

            return RedirectToAction("Index", "Inventory");
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.Id == id);
        }
    }
}
