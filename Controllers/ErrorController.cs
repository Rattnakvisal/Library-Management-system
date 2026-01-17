using Microsoft.AspNetCore.Mvc;

public class ErrorController : Controller
{
    [Route("Error/404")]
    public IActionResult PageNotFound()
    {
        Response.StatusCode = 404;
        return View();
    }

    [Route("Error/500")]
    public IActionResult ServerError()
    {
        Response.StatusCode = 500;
        return View();
    }
}
