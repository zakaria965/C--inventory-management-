using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InventoryManagementSystem.Data;
using InventoryManagementSystem.Filters;
using InventoryManagementSystem.Models;

namespace InventoryManagementSystem.Controllers
{
    [RequireAdminRole]
    public class EmployeeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public EmployeeController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Employee
        public async Task<IActionResult> Index(string searchString, string departmentFilter)
        {
            var employees = from e in _context.Employees select e;

            if (!string.IsNullOrEmpty(searchString))
            {
                employees = employees.Where(e => e.FirstName.Contains(searchString) 
                    || e.LastName.Contains(searchString)
                    || e.Email.Contains(searchString)
                    || (e.Position != null && e.Position.Contains(searchString)));
            }

            if (!string.IsNullOrEmpty(departmentFilter))
            {
                employees = employees.Where(e => e.Department == departmentFilter);
            }

            var departments = await _context.Employees
                .Where(e => e.Department != null)
                .Select(e => e.Department)
                .Distinct()
                .ToListAsync();

            ViewBag.Departments = departments;
            ViewBag.SearchString = searchString;
            ViewBag.DepartmentFilter = departmentFilter;

            return View(await employees.OrderBy(e => e.LastName).ThenBy(e => e.FirstName).ToListAsync());
        }

        // GET: Employee/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Employee/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Employee employee)
        {
            if (ModelState.IsValid)
            {
                employee.CreatedDate = DateTime.Now;
                _context.Employees.Add(employee);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Employee added successfully!";
                return RedirectToAction(nameof(Index));
            }
            return View(employee);
        }

        // GET: Employee/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
            {
                return NotFound();
            }
            return View(employee);
        }

        // POST: Employee/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Employee employee)
        {
            if (id != employee.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(employee);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Employee updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EmployeeExists(employee.Id))
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
            return View(employee);
        }

        // GET: Employee/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var employee = await _context.Employees
                .FirstOrDefaultAsync(m => m.Id == id);

            if (employee == null)
            {
                return NotFound();
            }

            return View(employee);
        }

        // GET: Employee/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var employee = await _context.Employees
                .FirstOrDefaultAsync(m => m.Id == id);

            if (employee == null)
            {
                return NotFound();
            }

            // Check for related records that prevent deletion
            var hasSalaries = await _context.Salaries.AnyAsync(s => s.EmployeeId == id);

            if (hasSalaries)
            {
                var salaryCount = await _context.Salaries.CountAsync(s => s.EmployeeId == id);
                ViewBag.HasRelatedRecords = true;
                ViewBag.RelatedRecords = new { Salaries = salaryCount };
            }
            else
            {
                ViewBag.HasRelatedRecords = false;
            }

            return View(employee);
        }

        // POST: Employee/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var employee = await _context.Employees.FindAsync(id);
            
            if (employee == null)
            {
                TempData["Error"] = "Employee not found.";
                return RedirectToAction(nameof(Index));
            }

            // Check for related records that prevent deletion
            var hasSalaries = await _context.Salaries.AnyAsync(s => s.EmployeeId == id);

            if (hasSalaries)
            {
                var salaryCount = await _context.Salaries.CountAsync(s => s.EmployeeId == id);
                TempData["Error"] = $"Cannot delete employee '{employee.FullName}' because they have {salaryCount} salary record(s). Please remove or update these records first.";
                return RedirectToAction(nameof(Index));
            }

            // No related records, safe to delete
            try
            {
                _context.Employees.Remove(employee);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Employee deleted successfully!";
            }
            catch (DbUpdateException ex)
            {
                TempData["Error"] = $"Error deleting employee: {ex.InnerException?.Message ?? ex.Message}. The employee may have related records that prevent deletion.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"An error occurred while deleting the employee: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Employee/ToggleStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee != null)
            {
                employee.IsActive = !employee.IsActive;
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Employee {(employee.IsActive ? "activated" : "deactivated")} successfully!";
            }
            return RedirectToAction(nameof(Index));
        }

        private bool EmployeeExists(int id)
        {
            return _context.Employees.Any(e => e.Id == id);
        }
    }
}
