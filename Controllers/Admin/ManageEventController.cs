using Microsoft.AspNetCore.Mvc;

namespace Library_Management_system.Controllers.Admin;

public class ManageEventController : Controller
{
    // GET
    [Route("admin/manageevent")]
    public IActionResult Index()
    {
        return View("~/Views/Admin/ManageEvent/Index.cshtml");
    }
}