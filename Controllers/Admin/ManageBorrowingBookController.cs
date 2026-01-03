using Microsoft.AspNetCore.Mvc;

namespace Library_Management_system.Controllers.Admin;

public class ManageBorrowingBookController : Controller
{
    // GET
    [Route("admin/manageborrowingbook")]
    public IActionResult Index()
    {
        return View("~/Views/Admin/ManageBorrowingBook/Index.cshtml");
    }
}