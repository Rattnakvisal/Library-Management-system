using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Library_Management_system.Models;
using System.Threading.Tasks;
using System.Text.Json; // Ensure this is available if needed

namespace Library_Management_system.Areas.Identity.Pages.Account;

[AllowAnonymous]
public class RegisterConfirmationModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;

    public RegisterConfirmationModel(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public string Email { get; set; } = string.Empty;

    public bool DisplayConfirmAccountLink { get; set; }

    public async Task<IActionResult> OnGetAsync(string email, string? returnUrl = null)
    {
        if (email == null)
        {
            return RedirectToPage("/Index");
        }

        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            return NotFound($"Unable to load user with email '{email}'.");
        }

        Email = email;

        // Set this to false so the link is NOT displayed on the page.
        // This forces the user to check their physical email inbox.
        DisplayConfirmAccountLink = false;

        return Page();
    }
}