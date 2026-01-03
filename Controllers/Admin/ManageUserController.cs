using Microsoft.AspNetCore.Mvc;

namespace Library_Management_system.Controllers.Admin;

public class ManageUserController : Controller
{
    // GET
    [Route("admin/manageuser")]
    public IActionResult Index()
    {
        return View("~/Views/Admin/ManageUser/Index.cshtml");
    }
}