using Microsoft.AspNetCore.Mvc;

namespace Library_Management_system.Controllers.Admin;

public class ManageBooksController : Controller
{
    // GET
    [Route("admin/managebooks")]
    public IActionResult Index()
    {
        return View("~/Views/Admin/ManageBooks/Index.cshtml");
    }
}