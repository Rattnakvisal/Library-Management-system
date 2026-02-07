using Microsoft.AspNetCore.Mvc;
namespace Library_Management_system.Controllers.user
{
    public class BooksController : Controller
    {
        [Route("user/books")]
        public IActionResult BookDetail()
        {
            //return View();
            return View("~/Views/User/Books/BookDetail.cshtml");
        }
    }
}
