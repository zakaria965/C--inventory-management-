using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InventoryManagementSystem.Data;

namespace InventoryManagementSystem.Controllers
{
    public class AboutController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AboutController(ApplicationDbContext context)
        {
            _context = context;
        }

        [Microsoft.AspNetCore.Authorization.AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            var missionVision = await _context.MissionVisions.FirstOrDefaultAsync();
            var teamProfiles = await _context.TeamProfiles.Where(t => t.IsActive).OrderBy(t => t.FullName).ToListAsync();
            
            ViewBag.MissionVision = missionVision;
            ViewBag.TeamProfiles = teamProfiles;
            
            return View();
        }
    }
}

