using Microsoft.AspNetCore.Mvc;

namespace Library_Management_system.Controllers.user
{
    public class BooksController : Controller
    {
        [HttpGet("user/books/{id:int?}")]
        public IActionResult BookDetail(int? id)
        {
            if (id.HasValue)
            {
                return RedirectToAction("BookDetail", "Home", new { id = id.Value });
            }

            return RedirectToAction("BookIndex", "Home");
        }
    }
}
