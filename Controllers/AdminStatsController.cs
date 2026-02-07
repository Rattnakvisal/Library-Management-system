using Library_Management_system.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace Library_Management_system.Controllers
{
    [Authorize(Roles = "Admin,Librarian")]
    [Route("admin/api/stats")]
    public class AdminStatsController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminStatsController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        [HttpGet("total-users")]
        public IActionResult TotalUsers()
        {
            var total = _userManager.Users.Count();
            return Json(new { ok = true, total });
        }
    }
}
