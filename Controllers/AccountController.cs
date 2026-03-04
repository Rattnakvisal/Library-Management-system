using Library_Management_system.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;

namespace Library_Management_system.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;

    public AccountController(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    [HttpGet("Account/ConfirmEmail")]
    public async Task<IActionResult> ConfirmEmail(string userId, string token)
    {
        if (userId == null || token == null)
        {
            return RedirectToAction("Index", "Home");
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound($"Unable to load user with ID '{userId}'.");
        }

        // Decode the token (Identity tokens often have '+' which can turn into spaces in URLs)
        var result = await _userManager.ConfirmEmailAsync(user, token);
        
        if (result.Succeeded)
        {
            ViewBag.Status = "Thank you for confirming your email. You can now log in.";
            return View();
        }

        ViewBag.Status = "Error confirming your email.";
        return View();
    }
}