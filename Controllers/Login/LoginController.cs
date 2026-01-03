using Microsoft.AspNetCore.Mvc;

namespace Library_Management_system.Controllers.Login
{
    public class LoginController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
