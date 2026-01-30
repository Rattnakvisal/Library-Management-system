using System.Diagnostics;
using Library_Management_system.Models;
using Microsoft.AspNetCore.Mvc;

namespace Library_Management_system.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet("about")]
        public IActionResult About()
        {
            ViewBag.Title = "About";
            return View("~/Views/User/About/about.cshtml");
        }

        [HttpGet("about/faq")]
        public IActionResult AboutFaq()
        {
            ViewBag.Title = "About - FAQ";
            return View("~/Views/User/About/faq.cshtml");
        }

        [HttpGet("about/support")]
        public IActionResult AboutSupport()
        {
            ViewBag.Title = "About - Support";
            return View("~/Views/User/About/support.cshtml");
        }

        [HttpGet("about/policies")]
        public IActionResult AboutPolicies()
        {
            ViewBag.Title = "About - Policies";
            return View("~/Views/User/About/policies.cshtml");
        }

        [HttpGet("about/account")]
        public IActionResult AboutAccount()
        {
            ViewBag.Title = "About - Account";
            return View("~/Views/User/About/account.cshtml");
        }

        [HttpGet("/login")]
        public IActionResult Login()
        {
            ViewBag.Title = "About - Login";
            return RedirectToPage("/Account/Login", new { area = "Identity" });
        }

        [HttpGet("event")]
        public IActionResult Event()
        {
            ViewBag.Title = "Events";
            return View("~/Views/User/Event/Event.cshtml");
        }

        [HttpGet("contact")]
        public IActionResult Contact()
        {
            ViewBag.Title = "Contact";
            return View("~/Views/User/Contact/Contact.cshtml");
        }

        [HttpGet("history")]
        public IActionResult History()
        {
            ViewBag.Title = "History";
            return View("~/Views/User/History/History.cshtml");
        }

        

        [HttpGet("cart")]
        public IActionResult Cart()
        {
            ViewBag.Title = "Cart";
            return View("~/Views/User/Cart/Cart.cshtml");
        }

        [HttpGet("profile")]
        public IActionResult Profile()
        {
            ViewBag.Title = "Profile";
            return View("~/Views/User/Profile/Profile.cshtml");
        }
        [HttpGet("bookmark")]
        public IActionResult Bookmark()
        {
            ViewBag.Title = "Profile";
            return View("~/Views/User/Bookmark/Bookmark.cshtml");
        }

        [HttpGet("cookie")]
        public IActionResult Cookie()
        {
            ViewBag.Title = "Cookie";
            return View("~/Views/User/Cookie/Cookie.cshtml");
        }
        //public IActionResult Category()
        //{
        //    return View("~/Views/User/Category/Category.cshtml");
        //}
        //[Area("User")]
            public IActionResult Category(int page = 1)
            {
                ViewBag.CurrentPage = page;
                return View("~/Views/User/Category/Category.cshtml");
            }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
    
}
