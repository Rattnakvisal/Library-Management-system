// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Library_Management_system.Models;
using Library_Management_system.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Library_Management_system.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class RegisterModel : PageModel
    {
        private const string GenderClaimType = "Gender";
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IUserStore<ApplicationUser> _userStore;
        private readonly IUserEmailStore<ApplicationUser> _emailStore;
        private readonly ITelegramNotifier _telegramNotifier;
        private readonly ILogger<RegisterModel> _logger;

        public RegisterModel(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IUserStore<ApplicationUser> userStore,
            SignInManager<ApplicationUser> signInManager,
            ITelegramNotifier telegramNotifier,
            ILogger<RegisterModel> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _signInManager = signInManager;
            _telegramNotifier = telegramNotifier;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }
        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        public class InputModel
        {
            [Required]
            [StringLength(100)]
            [Display(Name = "Name")]
            public string Name { get; set; }

            [Required]
            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; }

            [Required]
            [Display(Name = "Phone Number")]
            [RegularExpression(@"^\+?[0-9]{8,15}$", ErrorMessage = "Phone number must contain 8 to 15 digits.")]
            public string PhoneNumber { get; set; }

            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; }

            [Required]
            [Display(Name = "Gender")]
            public string Gender { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ReturnUrl = returnUrl;

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (!ModelState.IsValid)
                return Page();

            var user = CreateUser();

            // Save Full Name (can contain spaces)
            user.FullName = (Input.Name ?? "").Trim();
            user.CreatedBy = "Self Register";
            user.CreatedDate = DateTime.UtcNow;

            if (string.IsNullOrWhiteSpace(user.FullName))
            {
                ModelState.AddModelError(string.Empty, "Name is required.");
                return Page();
            }

            var normalizedPhone = NormalizePhone(Input.PhoneNumber);
            if (string.IsNullOrWhiteSpace(normalizedPhone))
            {
                ModelState.AddModelError(nameof(Input.PhoneNumber), "Phone number is required.");
                return Page();
            }

            var existingPhoneUser = await _userManager.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.PhoneNumber == normalizedPhone);
            if (existingPhoneUser != null)
            {
                ModelState.AddModelError(nameof(Input.PhoneNumber), "Phone number is already registered.");
                return Page();
            }

            user.PhoneNumber = normalizedPhone;
            user.PhoneNumberConfirmed = true;

            // Create valid UserName: letters + digits ONLY
            // "Chun Rattnakvisal" -> "ChunRattnakvisal"
            var baseUserName = Regex.Replace(user.FullName, @"[^a-zA-Z0-9]", "");

            if (string.IsNullOrWhiteSpace(baseUserName))
            {
                ModelState.AddModelError(string.Empty, "Name must contain letters or digits.");
                return Page();
            }

            // Make UserName unique
            var userName = baseUserName;
            var i = 1;
            while (await _userManager.FindByNameAsync(userName) != null)
            {
                i++;
                userName = $"{baseUserName}{i}";
            }

            await _userStore.SetUserNameAsync(user, userName, CancellationToken.None);

            // Store email
            await _emailStore.SetEmailAsync(user, (Input.Email ?? "").Trim(), CancellationToken.None);

            // Create user
            var result = await _userManager.CreateAsync(user, Input.Password);

            if (result.Succeeded)
            {
                _logger.LogInformation("User created a new account with password.");

                // AUTO CONFIRM EMAIL
                user.EmailConfirmed = true;
                await _userManager.UpdateAsync(user);

                const string memberRole = "User";

                if (!await _roleManager.RoleExistsAsync(memberRole))
                {
                    var createRoleResult = await _roleManager.CreateAsync(new IdentityRole(memberRole));
                    if (!createRoleResult.Succeeded)
                    {
                        foreach (var error in createRoleResult.Errors)
                            ModelState.AddModelError(string.Empty, error.Description);

                        await _userManager.DeleteAsync(user);
                        return Page();
                    }
                }

                var addToRoleResult = await _userManager.AddToRoleAsync(user, memberRole);
                if (!addToRoleResult.Succeeded)
                {
                    foreach (var error in addToRoleResult.Errors)
                        ModelState.AddModelError(string.Empty, error.Description);

                    await _userManager.DeleteAsync(user);
                    return Page();
                }

                var genderClaimResult = await _userManager.AddClaimAsync(
                    user,
                    new Claim(GenderClaimType, NormalizeGenderForStore(Input.Gender)));
                if (!genderClaimResult.Succeeded)
                {
                    foreach (var error in genderClaimResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }

                    await _userManager.DeleteAsync(user);
                    return Page();
                }

                var approvalClaimResult = await _userManager.AddClaimAsync(
                    user,
                    new Claim(AccountApproval.ClaimType, AccountApproval.Pending));
                if (!approvalClaimResult.Succeeded)
                {
                    foreach (var error in approvalClaimResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }

                    await _userManager.DeleteAsync(user);
                    return Page();
                }

                var lockoutEnabledResult = await _userManager.SetLockoutEnabledAsync(user, true);
                if (!lockoutEnabledResult.Succeeded)
                {
                    foreach (var error in lockoutEnabledResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }

                    await _userManager.DeleteAsync(user);
                    return Page();
                }

                var lockoutUntilApprovedResult = await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
                if (!lockoutUntilApprovedResult.Succeeded)
                {
                    foreach (var error in lockoutUntilApprovedResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }

                    await _userManager.DeleteAsync(user);
                    return Page();
                }

                var registeredUtc = DateTime.UtcNow;
                var registerAlert = string.Join('\n',
                    "New user registration pending approval.",
                    $"Name: {user.FullName}",
                    $"Email: {user.Email}",
                    $"Phone: {user.PhoneNumber}",
                    $"Gender: {GetDisplayGender(Input.Gender)}",
                    $"Registered (UTC): {registeredUtc:yyyy-MM-dd HH:mm:ss}");
                await _telegramNotifier.SendAdminAlertAsync(registerAlert);

                TempData["InfoMessage"] = "Register completed. Please wait for admin approval before login.";

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return RedirectToPage("./Login", new { returnUrl });
                }

                return RedirectToPage("./Login");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return Page();
        }

        private ApplicationUser CreateUser()
        {
            try
            {
                return Activator.CreateInstance<ApplicationUser>();
            }
            catch
            {
                throw new InvalidOperationException($"Can't create an instance of '{nameof(ApplicationUser)}'. " +
                    $"Ensure that '{nameof(ApplicationUser)}' is not an abstract class and has a parameterless constructor.");
            }
        }

        private IUserEmailStore<ApplicationUser> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
                throw new NotSupportedException("The default UI requires a user store with email support.");

            return (IUserEmailStore<ApplicationUser>)_userStore;
        }

        private static string NormalizeGenderForStore(string gender)
        {
            if (string.Equals(gender, "M", StringComparison.OrdinalIgnoreCase)
                || string.Equals(gender, "Male", StringComparison.OrdinalIgnoreCase))
            {
                return "M";
            }

            if (string.Equals(gender, "F", StringComparison.OrdinalIgnoreCase)
                || string.Equals(gender, "Female", StringComparison.OrdinalIgnoreCase))
            {
                return "F";
            }

            return "U";
        }

        private static string GetDisplayGender(string gender)
        {
            if (string.Equals(gender, "M", StringComparison.OrdinalIgnoreCase)
                || string.Equals(gender, "Male", StringComparison.OrdinalIgnoreCase))
            {
                return "Male";
            }

            if (string.Equals(gender, "F", StringComparison.OrdinalIgnoreCase)
                || string.Equals(gender, "Female", StringComparison.OrdinalIgnoreCase))
            {
                return "Female";
            }

            return "Unspecified";
        }

        private static string NormalizePhone(string phoneNumber)
        {
            return new string((phoneNumber ?? string.Empty).Where(char.IsDigit).ToArray());
        }
    }
}
