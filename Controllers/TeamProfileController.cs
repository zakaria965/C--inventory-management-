using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using InventoryManagementSystem.Data;
using InventoryManagementSystem.Models;

namespace InventoryManagementSystem.Controllers
{
    public class TeamProfileController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public TeamProfileController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // GET: TeamProfile
        public async Task<IActionResult> Index()
        {
            return View(await _context.TeamProfiles.OrderByDescending(t => t.CreatedDate).ToListAsync());
        }

        // GET: TeamProfile/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: TeamProfile/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TeamProfile teamProfile, IFormFile? profileImage)
        {
            if (ModelState.IsValid)
            {
                // Handle image upload
                if (profileImage != null && profileImage.Length > 0)
                {
                    var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "team-profiles");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    var uniqueFileName = Guid.NewGuid().ToString() + "_" + profileImage.FileName;
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await profileImage.CopyToAsync(fileStream);
                    }

                    teamProfile.ProfileImagePath = "/uploads/team-profiles/" + uniqueFileName;
                }

                teamProfile.CreatedDate = DateTime.Now;
                teamProfile.UpdatedDate = DateTime.Now;
                _context.TeamProfiles.Add(teamProfile);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Team profile created successfully!";
                return RedirectToAction(nameof(Index));
            }
            return View(teamProfile);
        }

        // GET: TeamProfile/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var teamProfile = await _context.TeamProfiles.FindAsync(id);
            if (teamProfile == null)
            {
                return NotFound();
            }
            return View(teamProfile);
        }

        // POST: TeamProfile/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, TeamProfile teamProfile, IFormFile? profileImage)
        {
            if (id != teamProfile.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
            try
            {
                // Get existing record to preserve image path if no new image is uploaded
                var existing = await _context.TeamProfiles.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id);
                
                // Handle image upload
                if (profileImage != null && profileImage.Length > 0)
                {
                    // Delete old image if exists
                    if (existing != null && !string.IsNullOrEmpty(existing.ProfileImagePath))
                    {
                        var oldImagePath = Path.Combine(_environment.WebRootPath, existing.ProfileImagePath.TrimStart('/'));
                        if (System.IO.File.Exists(oldImagePath))
                        {
                            System.IO.File.Delete(oldImagePath);
                        }
                    }

                    var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "team-profiles");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    var uniqueFileName = Guid.NewGuid().ToString() + "_" + profileImage.FileName;
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await profileImage.CopyToAsync(fileStream);
                    }

                    teamProfile.ProfileImagePath = "/uploads/team-profiles/" + uniqueFileName;
                }
                else
                {
                    // Keep existing image path
                    if (existing != null)
                    {
                        teamProfile.ProfileImagePath = existing.ProfileImagePath;
                    }
                }

                    teamProfile.UpdatedDate = DateTime.Now;
                    _context.Update(teamProfile);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Team profile updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TeamProfileExists(teamProfile.Id))
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
            return View(teamProfile);
        }

        // GET: TeamProfile/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var teamProfile = await _context.TeamProfiles
                .FirstOrDefaultAsync(m => m.Id == id);
            if (teamProfile == null)
            {
                return NotFound();
            }

            return View(teamProfile);
        }

        // POST: TeamProfile/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var teamProfile = await _context.TeamProfiles.FindAsync(id);
            if (teamProfile != null)
            {
                // Delete image file if exists
                if (!string.IsNullOrEmpty(teamProfile.ProfileImagePath))
                {
                    var imagePath = Path.Combine(_environment.WebRootPath, teamProfile.ProfileImagePath.TrimStart('/'));
                    if (System.IO.File.Exists(imagePath))
                    {
                        System.IO.File.Delete(imagePath);
                    }
                }

                _context.TeamProfiles.Remove(teamProfile);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Team profile deleted successfully!";
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: TeamProfile/ToggleStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var teamProfile = await _context.TeamProfiles.FindAsync(id);
            if (teamProfile != null)
            {
                teamProfile.IsActive = !teamProfile.IsActive;
                teamProfile.UpdatedDate = DateTime.Now;
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Team profile {(teamProfile.IsActive ? "activated" : "deactivated")} successfully!";
            }
            return RedirectToAction(nameof(Index));
        }

        private bool TeamProfileExists(int id)
        {
            return _context.TeamProfiles.Any(e => e.Id == id);
        }
    }
}

