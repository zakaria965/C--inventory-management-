using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using InventoryManagementSystem.Data;
using InventoryManagementSystem.Models;

namespace InventoryManagementSystem.Controllers
{
    public class SupplierController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SupplierController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Supplier
        public async Task<IActionResult> Index(string searchString, string categoryFilter, string statusFilter)
        {
            var suppliers = from s in _context.Suppliers select s;

            if (!string.IsNullOrEmpty(searchString))
            {
                suppliers = suppliers.Where(s => s.Name.Contains(searchString) 
                    || (s.ContactPersonName != null && s.ContactPersonName.Contains(searchString))
                    || (s.EmailAddress != null && s.EmailAddress.Contains(searchString))
                    || (s.PhoneNumber != null && s.PhoneNumber.Contains(searchString)));
            }

            if (!string.IsNullOrEmpty(categoryFilter))
            {
                suppliers = suppliers.Where(s => s.Category == categoryFilter);
            }

            if (!string.IsNullOrEmpty(statusFilter))
            {
                bool isActive = statusFilter == "Active";
                suppliers = suppliers.Where(s => s.IsActive == isActive);
            }

            List<string> categories;
            if (await _context.Categories.AnyAsync())
            {
                categories = await _context.Categories.OrderBy(c => c.Name).Select(c => c.Name).ToListAsync();
            }
            else
            {
                categories = await _context.Suppliers
                    .Where(s => s.Category != null)
                    .Select(s => s.Category)
                    .Distinct()
                    .ToListAsync();
            }

            ViewBag.Categories = categories;
            ViewBag.SearchString = searchString;
            ViewBag.CategoryFilter = categoryFilter;
            ViewBag.StatusFilter = statusFilter;

            return View(await suppliers.OrderBy(s => s.Name).ToListAsync());
        }

        // GET: Supplier/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var supplier = await _context.Suppliers
                .FirstOrDefaultAsync(m => m.Id == id);

            if (supplier == null)
            {
                return NotFound();
            }

            return View(supplier);
        }

        // GET: Supplier/Create
        public IActionResult Create()
        {
            ViewBag.Categories = _context.Categories.OrderBy(c => c.Name).Select(c => c.Name).ToList();
            var role = HttpContext.Session.GetString("UserRole") ?? "User";
            ViewBag.IsAdmin = role.ToLower() == "admin";
            return View();
        }

        // POST: Supplier/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Supplier supplier)
        {
            // Check for duplicate supplier name
            if (await _context.Suppliers.AnyAsync(s => s.Name == supplier.Name))
            {
                ModelState.AddModelError("Name", "A supplier with this name already exists.");
            }

            if (ModelState.IsValid)
            {
                supplier.CreatedDate = DateTime.Now;
                _context.Suppliers.Add(supplier);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Supplier created successfully!";
                return RedirectToAction(nameof(Index));
            }
            return View(supplier);
        }

        // GET: Supplier/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier == null)
            {
                return NotFound();
            }
            ViewBag.Categories = _context.Categories.OrderBy(c => c.Name).Select(c => c.Name).ToList();
            var role = HttpContext.Session.GetString("UserRole") ?? "User";
            ViewBag.IsAdmin = role.ToLower() == "admin";
            return View(supplier);
        }

        // POST: Supplier/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Supplier supplier)
        {
            if (id != supplier.Id)
            {
                return NotFound();
            }

            // Check for duplicate supplier name (excluding current supplier)
            if (await _context.Suppliers.AnyAsync(s => s.Name == supplier.Name && s.Id != id))
            {
                ModelState.AddModelError("Name", "A supplier with this name already exists.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    supplier.LastUpdated = DateTime.Now;
                    _context.Update(supplier);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Supplier updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SupplierExists(supplier.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(supplier);
        }

        // POST: Supplier/ToggleStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier != null)
            {
                supplier.IsActive = !supplier.IsActive;
                supplier.LastUpdated = DateTime.Now;
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Supplier {(supplier.IsActive ? "activated" : "deactivated")} successfully!";
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: Supplier/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var supplier = await _context.Suppliers
                .FirstOrDefaultAsync(m => m.Id == id);

            if (supplier == null)
            {
                return NotFound();
            }

            return View(supplier);
        }

        // POST: Supplier/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier != null)
            {
                // Check if supplier has associated products
                var hasProducts = await _context.Products.AnyAsync(p => p.Supplier == supplier.Name);
                
                if (hasProducts)
                {
                    TempData["Error"] = "Cannot delete supplier. There are products associated with this supplier.";
                    return RedirectToAction(nameof(Index));
                }

                _context.Suppliers.Remove(supplier);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Supplier deleted successfully!";
            }
            return RedirectToAction(nameof(Index));
        }

        private bool SupplierExists(int id)
        {
            return _context.Suppliers.Any(e => e.Id == id);
        }
    }
}

