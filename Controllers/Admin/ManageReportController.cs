using Microsoft.AspNetCore.Mvc;

namespace Library_Management_system.Controllers.Admin;

public class ManageReportController : Controller
{
    // GET
    [Route("admin/managereport")]
    public IActionResult Index()
    {
        return View("~/Views/Admin/ManageReport/Index.cshtml");
    }
}