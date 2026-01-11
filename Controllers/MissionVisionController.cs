using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InventoryManagementSystem.Data;
using InventoryManagementSystem.Models;

namespace InventoryManagementSystem.Controllers
{
    public class MissionVisionController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public MissionVisionController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // GET: MissionVision
        public async Task<IActionResult> Index()
        {
            var missionVision = await _context.MissionVisions.FirstOrDefaultAsync();
            return View(missionVision);
        }

        // GET: MissionVision/Create
        public IActionResult Create()
        {
            // Check if one already exists
            var existing = _context.MissionVisions.FirstOrDefault();
            if (existing != null)
            {
                return RedirectToAction(nameof(Edit), new { id = existing.Id });
            }
            return View();
        }

        // POST: MissionVision/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MissionVision missionVision)
        {
            // Check if one already exists
            var existing = await _context.MissionVisions.FirstOrDefaultAsync();
            if (existing != null)
            {
                ModelState.AddModelError("", "A Mission & Vision record already exists. Please edit the existing one.");
                return View(missionVision);
            }

            if (ModelState.IsValid)
            {
                missionVision.LastUpdated = DateTime.Now;
                _context.MissionVisions.Add(missionVision);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Mission & Vision created successfully!";
                return RedirectToAction(nameof(Index));
            }
            return View(missionVision);
        }

        // GET: MissionVision/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var missionVision = await _context.MissionVisions.FindAsync(id);
            if (missionVision == null)
            {
                return NotFound();
            }
            return View(missionVision);
        }

        // POST: MissionVision/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, MissionVision missionVision)
        {
            if (id != missionVision.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    missionVision.LastUpdated = DateTime.Now;
                    _context.Update(missionVision);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Mission & Vision updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MissionVisionExists(missionVision.Id))
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
            return View(missionVision);
        }

        private bool MissionVisionExists(int id)
        {
            return _context.MissionVisions.Any(e => e.Id == id);
        }
    }
}


