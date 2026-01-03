using Microsoft.AspNetCore.Mvc;

namespace Library_Management_system.Controllers.Admin;

public class ManageFeedbackController : Controller
{
    // GET
    [Route("admin/managefeedback")]
    public IActionResult Index()
    {
        return View("~/Views/Admin/ManageFeedback/Index.cshtml");
    }
}