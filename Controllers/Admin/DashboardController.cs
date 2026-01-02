using Microsoft.AspNetCore.Mvc;

namespace Library_Management_system.Controllers.Admin
{
    [Route("admin/dashboard")]
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            return View("~/Views/Admin/Dashboard/Index.cshtml");
        }
    }
}
