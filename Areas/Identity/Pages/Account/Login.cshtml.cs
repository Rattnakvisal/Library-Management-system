#nullable disable
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Library_Management_system.Models;
using Library_Management_system.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Library_Management_system.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class LoginModel : PageModel
    {
        private static readonly TimeSpan LoginOtpLifetime = TimeSpan.FromMinutes(10);

        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ITelegramNotifier _telegramNotifier;
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<LoginModel> _logger;

        public LoginModel(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            ITelegramNotifier telegramNotifier,
            IMemoryCache memoryCache,
            ILogger<LoginModel> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _telegramNotifier = telegramNotifier;
            _memoryCache = memoryCache;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; }
        public string ReturnUrl { get; set; }

        [TempData]
        public string ErrorMessage { get; set; }

        [TempData]
        public string InfoMessage { get; set; }

        public class InputModel
        {
            [Required]
            [Display(Name = "Email or Phone")]
            public string Email { get; set; }

            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            [Display(Name = "Remember me?")]
            public bool RememberMe { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
                ModelState.AddModelError(string.Empty, ErrorMessage);

            returnUrl ??= Url.Content("~/");
            ReturnUrl = returnUrl;

            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ReturnUrl = returnUrl;

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (!ModelState.IsValid)
                return Page();

            var loginIdentifier = (Input.Email ?? string.Empty).Trim();
            var user = await _userManager.FindByEmailAsync(loginIdentifier);
            if (user == null)
            {
                user = await FindUserByPhoneAsync(loginIdentifier);
            }

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return Page();
            }

            var approvalStatus = await GetApprovalStatusAsync(user);
            if (approvalStatus == AccountApproval.Pending)
            {
                ModelState.AddModelError(string.Empty, "Your account is pending admin approval.");
                return Page();
            }

            if (approvalStatus == AccountApproval.Canceled)
            {
                ModelState.AddModelError(string.Empty, "Your account access was canceled by admin.");
                return Page();
            }

            var result = await _signInManager.CheckPasswordSignInAsync(
                user,
                Input.Password,
                lockoutOnFailure: false);

            if (result.Succeeded)
            {
                return await StartLoginOtpAsync(user, returnUrl);
            }

            if (result.IsNotAllowed && !user.EmailConfirmed)
            {
                user.EmailConfirmed = true;
                var confirmResult = await _userManager.UpdateAsync(user);
                if (confirmResult.Succeeded)
                {
                    var retryResult = await _signInManager.CheckPasswordSignInAsync(
                        user,
                        Input.Password,
                        lockoutOnFailure: false);

                    if (retryResult.Succeeded)
                    {
                        return await StartLoginOtpAsync(user, returnUrl);
                    }
                }
            }

            if (result.IsLockedOut)
            {
                ModelState.AddModelError(string.Empty, "Your account is currently locked. Please contact admin.");
                return Page();
            }

            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return Page();
        }

        private async Task<IActionResult> StartLoginOtpAsync(ApplicationUser user, string returnUrl)
        {
            var normalizedPhone = PhoneNumberHelper.NormalizeForCambodia(user.PhoneNumber ?? string.Empty);
            if (string.IsNullOrWhiteSpace(normalizedPhone))
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Your account has no phone number for Telegram OTP. Please contact admin.");
                return Page();
            }

            var linkedChatId = await ResolveLinkedTelegramChatIdAsync(user, normalizedPhone);
            if (string.IsNullOrWhiteSpace(linkedChatId))
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Telegram account is not linked for this phone number. Open the OTP bot, tap Start, then send your phone number.");
                return Page();
            }

            var requestId = Guid.NewGuid().ToString("N");
            var otpCode = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
            var expiresUtc = DateTime.UtcNow.Add(LoginOtpLifetime);
            var safeReturnUrl = !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
                ? returnUrl
                : Url.Content("~/");

            var loginRequest = new PendingLoginOtpRequest
            {
                RequestId = requestId,
                UserId = user.Id,
                PhoneNumber = normalizedPhone,
                OtpCode = otpCode,
                RememberMe = Input.RememberMe,
                ReturnUrl = safeReturnUrl,
                ExpiresUtc = expiresUtc
            };

            var cacheKey = PendingLoginOtpRequest.BuildCacheKey(requestId);
            _memoryCache.Set(cacheKey, loginRequest, expiresUtc);

            user.TelegramChatId = linkedChatId;
            user.TelegramLinkedPhone = normalizedPhone;
            user.TelegramLinkedAtUtc ??= DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            var sent = await _telegramNotifier.SendLoginOtpToChatAsync(
                linkedChatId,
                normalizedPhone,
                otpCode,
                expiresUtc);

            if (!sent)
            {
                _memoryCache.Remove(cacheKey);
                ModelState.AddModelError(string.Empty, "Failed to send login OTP to Telegram. Please try again.");
                return Page();
            }

            _logger.LogInformation("Login OTP sent for user {UserId}.", user.Id);
            return RedirectToPage("./LoginOtp", new { requestId });
        }

        private async Task<ApplicationUser> FindUserByPhoneAsync(string rawPhoneNumber)
        {
            if (!PhoneNumberHelper.LooksLikePhone(rawPhoneNumber))
            {
                return null;
            }

            var candidates = PhoneNumberHelper.BuildMatchCandidates(rawPhoneNumber).ToArray();
            if (candidates.Length == 0)
            {
                return null;
            }

            var user = await _userManager.Users
                .FirstOrDefaultAsync(u => u.PhoneNumber != null && candidates.Contains(u.PhoneNumber));
            if (user != null)
            {
                return user;
            }

            var phoneCandidates = await _userManager.Users
                .Where(u => u.PhoneNumber != null)
                .ToListAsync();

            return phoneCandidates.FirstOrDefault(u => PhoneNumberHelper.AreEquivalent(u.PhoneNumber, rawPhoneNumber));
        }

        private async Task<string> ResolveLinkedTelegramChatIdAsync(ApplicationUser user, string normalizedPhone)
        {
            if (!string.IsNullOrWhiteSpace(user.TelegramChatId) &&
                (string.IsNullOrWhiteSpace(user.TelegramLinkedPhone) ||
                 PhoneNumberHelper.AreEquivalent(user.TelegramLinkedPhone, normalizedPhone)))
            {
                return user.TelegramChatId;
            }

            return await _telegramNotifier.FindUserChatIdByPhoneAsync(normalizedPhone) ?? string.Empty;
        }

        private async Task<string> GetApprovalStatusAsync(ApplicationUser user)
        {
            var claims = await _userManager.GetClaimsAsync(user);
            var rawValue = claims
                .FirstOrDefault(c => c.Type == AccountApproval.ClaimType)
                ?.Value;

            return AccountApproval.Normalize(rawValue);
        }
    }
}
