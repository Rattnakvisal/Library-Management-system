using Microsoft.AspNetCore.Mvc;

namespace Library_Management_system.Controllers.Admin;

public class ManageUserController : Controller
{
    // GET
    public IActionResult Index()
    {
        return View();
    }
}