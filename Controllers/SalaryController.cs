using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using InventoryManagementSystem.Data;
using InventoryManagementSystem.Models;

namespace InventoryManagementSystem.Controllers
{
    public class SalaryController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SalaryController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Salary
        public async Task<IActionResult> Index(string searchString, int? month, int? year, bool? isPaid)
        {
            var salaries = from s in _context.Salaries.Include(s => s.Employee) select s;

            if (!string.IsNullOrEmpty(searchString))
            {
                salaries = salaries.Where(s => s.Employee != null && 
                    (s.Employee.FirstName.Contains(searchString) || 
                     s.Employee.LastName.Contains(searchString) ||
                     s.Employee.Email.Contains(searchString)));
            }

            if (month.HasValue)
            {
                salaries = salaries.Where(s => s.Month == month.Value);
            }

            if (year.HasValue)
            {
                salaries = salaries.Where(s => s.Year == year.Value);
            }

            if (isPaid.HasValue)
            {
                salaries = salaries.Where(s => s.IsPaid == isPaid.Value);
            }

            // Get the salary list first
            var salaryList = await salaries.OrderByDescending(s => s.Year).ThenByDescending(s => s.Month).ToListAsync();
            
            // Update NetSalary for any records that might not have it calculated (for existing records)
            bool needsUpdate = false;
            foreach (var salary in salaryList)
            {
                if (salary.NetSalary == 0 && salary.SalaryAmount > 0)
                {
                    salary.NetSalary = salary.SalaryAmount - salary.AdvanceAmount;
                    needsUpdate = true;
                }
            }
            
            if (needsUpdate)
            {
                await _context.SaveChangesAsync();
            }

            // Calculate totals (using NetSalary to account for advances)
            var totalPaid = salaryList.Where(s => s.IsPaid).Sum(s => s.NetSalary);
            var totalUnpaid = salaryList.Where(s => !s.IsPaid).Sum(s => s.NetSalary);

            ViewBag.TotalPaid = totalPaid;
            ViewBag.TotalUnpaid = totalUnpaid;
            ViewBag.SearchString = searchString;
            ViewBag.Month = month;
            ViewBag.Year = year;
            ViewBag.IsPaid = isPaid;

            return View(salaryList);
        }

        // GET: Salary/Create
        public IActionResult Create()
        {
            ViewData["EmployeeId"] = new SelectList(_context.Employees.Where(e => e.IsActive).OrderBy(e => e.LastName), "Id", "FullName");
            return View();
        }

        // POST: Salary/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Salary salary)
        {
            if (ModelState.IsValid)
            {
                // Calculate salary for hourly workers
                if (salary.SalaryType == "Hourly" && salary.HoursWorked.HasValue)
                {
                    salary.SalaryAmount = salary.SalaryAmount * salary.HoursWorked.Value;
                }

                // Calculate net salary (subtract advance from salary amount)
                salary.NetSalary = salary.SalaryAmount - salary.AdvanceAmount;
                
                // Validate that advance doesn't exceed salary
                if (salary.AdvanceAmount > salary.SalaryAmount)
                {
                    ModelState.AddModelError("AdvanceAmount", "Advance amount cannot exceed salary amount.");
                    ViewData["EmployeeId"] = new SelectList(_context.Employees.Where(e => e.IsActive).OrderBy(e => e.LastName), "Id", "FullName", salary.EmployeeId);
                    return View(salary);
                }

                salary.CreatedDate = DateTime.Now;
                if (salary.IsPaid)
                {
                    salary.PaymentDate = DateTime.Now;
                }

                _context.Salaries.Add(salary);
                await _context.SaveChangesAsync();
                
                string successMessage = "Salary record created successfully!";
                if (salary.AdvanceAmount > 0)
                {
                    successMessage += $" Advance of ${salary.AdvanceAmount:N2} deducted. Net salary: ${salary.NetSalary:N2}";
                }
                TempData["Success"] = successMessage;
                return RedirectToAction(nameof(Index));
            }
            ViewData["EmployeeId"] = new SelectList(_context.Employees.Where(e => e.IsActive).OrderBy(e => e.LastName), "Id", "FullName", salary.EmployeeId);
            return View(salary);
        }

        // GET: Salary/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var salary = await _context.Salaries
                .Include(s => s.Employee)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (salary == null)
            {
                return NotFound();
            }

            ViewData["EmployeeId"] = new SelectList(_context.Employees.Where(e => e.IsActive).OrderBy(e => e.LastName), "Id", "FullName", salary.EmployeeId);
            return View(salary);
        }

        // POST: Salary/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Salary salary)
        {
            if (id != salary.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                // Calculate salary for hourly workers
                if (salary.SalaryType == "Hourly" && salary.HoursWorked.HasValue)
                {
                    salary.SalaryAmount = salary.SalaryAmount * salary.HoursWorked.Value;
                }

                // Calculate net salary (subtract advance from salary amount)
                salary.NetSalary = salary.SalaryAmount - salary.AdvanceAmount;
                
                // Validate that advance doesn't exceed salary
                if (salary.AdvanceAmount > salary.SalaryAmount)
                {
                    ModelState.AddModelError("AdvanceAmount", "Advance amount cannot exceed salary amount.");
                    ViewData["EmployeeId"] = new SelectList(_context.Employees.Where(e => e.IsActive).OrderBy(e => e.LastName), "Id", "FullName", salary.EmployeeId);
                    return View(salary);
                }

                // Set payment date if marked as paid
                if (salary.IsPaid && !salary.PaymentDate.HasValue)
                {
                    salary.PaymentDate = DateTime.Now;
                }

                try
                {
                    _context.Update(salary);
                    await _context.SaveChangesAsync();
                    
                    string successMessage = "Salary record updated successfully!";
                    if (salary.AdvanceAmount > 0)
                    {
                        successMessage += $" Advance of ${salary.AdvanceAmount:N2} deducted. Net salary: ${salary.NetSalary:N2}";
                    }
                    TempData["Success"] = successMessage;
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SalaryExists(salary.Id))
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
            ViewData["EmployeeId"] = new SelectList(_context.Employees.Where(e => e.IsActive).OrderBy(e => e.LastName), "Id", "FullName", salary.EmployeeId);
            return View(salary);
        }

        // POST: Salary/MarkAsPaid/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsPaid(int id)
        {
            var salary = await _context.Salaries.FindAsync(id);
            if (salary != null)
            {
                salary.IsPaid = true;
                salary.PaymentDate = DateTime.Now;
                await _context.SaveChangesAsync();
                TempData["Success"] = "Salary marked as paid!";
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: Salary/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var salary = await _context.Salaries
                .Include(s => s.Employee)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (salary == null)
            {
                return NotFound();
            }

            return View(salary);
        }

        // GET: Salary/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var salary = await _context.Salaries
                .Include(s => s.Employee)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (salary == null)
            {
                return NotFound();
            }

            return View(salary);
        }

        // POST: Salary/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var salary = await _context.Salaries.FindAsync(id);
            if (salary != null)
            {
                _context.Salaries.Remove(salary);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Salary record deleted successfully!";
            }
            return RedirectToAction(nameof(Index));
        }

        private bool SalaryExists(int id)
        {
            return _context.Salaries.Any(e => e.Id == id);
        }
    }
}

