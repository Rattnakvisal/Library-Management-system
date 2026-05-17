#nullable disable
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Library_Management_system.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Library_Management_system.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class LoginOtpModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<LoginOtpModel> _logger;

        public LoginOtpModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IMemoryCache memoryCache,
            ILogger<LoginOtpModel> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _memoryCache = memoryCache;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string PhoneNumberMask { get; set; }

        public class InputModel
        {
            [Required]
            public string RequestId { get; set; }

            [Required]
            [Display(Name = "OTP Code")]
            [RegularExpression(@"^[0-9]{6}$", ErrorMessage = "OTP code must be 6 digits.")]
            public string OtpCode { get; set; }
        }

        public IActionResult OnGet(string requestId = null)
        {
            if (!TryGetPendingRequest(requestId, out var loginRequest))
            {
                TempData["ErrorMessage"] = "Login OTP is invalid or expired. Please sign in again.";
                return RedirectToPage("./Login");
            }

            Input = new InputModel
            {
                RequestId = loginRequest.RequestId
            };

            PhoneNumberMask = MaskPhoneNumber(loginRequest.PhoneNumber);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (TryGetPendingRequest(Input?.RequestId, out var pendingForDisplay))
            {
                PhoneNumberMask = MaskPhoneNumber(pendingForDisplay.PhoneNumber);
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            if (!TryGetPendingRequest(Input.RequestId, out var loginRequest))
            {
                ModelState.AddModelError(string.Empty, "Login OTP has expired. Please sign in again.");
                return Page();
            }

            PhoneNumberMask = MaskPhoneNumber(loginRequest.PhoneNumber);

            if (!string.Equals(loginRequest.OtpCode, Input.OtpCode?.Trim(), StringComparison.Ordinal))
            {
                ModelState.AddModelError(nameof(Input.OtpCode), "OTP code is incorrect.");
                return Page();
            }

            var user = await _userManager.FindByIdAsync(loginRequest.UserId);
            if (user == null)
            {
                _memoryCache.Remove(PendingLoginOtpRequest.BuildCacheKey(loginRequest.RequestId));
                TempData["ErrorMessage"] = "Login OTP is invalid. Please sign in again.";
                return RedirectToPage("./Login");
            }

            await _signInManager.SignInAsync(user, loginRequest.RememberMe);
            _memoryCache.Remove(PendingLoginOtpRequest.BuildCacheKey(loginRequest.RequestId));
            _logger.LogInformation("User logged in with Telegram OTP.");

            if (await _userManager.IsInRoleAsync(user, "Admin") ||
                await _userManager.IsInRoleAsync(user, "Librarian"))
            {
                return Redirect("/admin/dashboard");
            }

            if (!string.IsNullOrWhiteSpace(loginRequest.ReturnUrl) && Url.IsLocalUrl(loginRequest.ReturnUrl))
            {
                return LocalRedirect(loginRequest.ReturnUrl);
            }

            return RedirectToAction("Index", "Home");
        }

        private bool TryGetPendingRequest(string requestId, out PendingLoginOtpRequest loginRequest)
        {
            loginRequest = null;
            if (string.IsNullOrWhiteSpace(requestId))
            {
                return false;
            }

            var cacheKey = PendingLoginOtpRequest.BuildCacheKey(requestId);
            if (!_memoryCache.TryGetValue(cacheKey, out PendingLoginOtpRequest cached) || cached == null)
            {
                return false;
            }

            if (cached.ExpiresUtc <= DateTime.UtcNow)
            {
                _memoryCache.Remove(cacheKey);
                return false;
            }

            loginRequest = cached;
            return true;
        }

        private static string MaskPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber) || phoneNumber.Length <= 4)
            {
                return phoneNumber ?? string.Empty;
            }

            var suffix = phoneNumber[^4..];
            return $"***{suffix}";
        }
    }
}
