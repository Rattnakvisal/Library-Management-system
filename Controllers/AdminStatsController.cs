using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Library_Management_system.Models;

namespace Library_Management_system.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("admin/api/stats")]
    public class AdminStatsController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminStatsController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        // GET: /admin/api/stats/total-users
        [HttpGet("total-users")]
        public IActionResult TotalUsers()
        {
            // Identity Users table
            var total = _userManager.Users.Count();
            return Json(new { ok = true, total });
        }
    }
}
