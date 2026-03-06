// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Library_Management_system.Models;
using Library_Management_system.Services;
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
    public class ForgotPasswordModel : PageModel
    {
        private static readonly TimeSpan OtpLifetime = TimeSpan.FromMinutes(10);

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ITelegramNotifier _telegramNotifier;
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<ForgotPasswordModel> _logger;

        public ForgotPasswordModel(
            UserManager<ApplicationUser> userManager,
            ITelegramNotifier telegramNotifier,
            IMemoryCache memoryCache,
            ILogger<ForgotPasswordModel> logger)
        {
            _userManager = userManager;
            _telegramNotifier = telegramNotifier;
            _memoryCache = memoryCache;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required]
            [Display(Name = "Phone Number")]
            [RegularExpression(@"^\+?[0-9]{8,15}$", ErrorMessage = "Phone number must contain 8 to 15 digits.")]
            public string PhoneNumber { get; set; }

            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "New Password")]
            public string Password { get; set; }

            [Required]
            [DataType(DataType.Password)]
            [Display(Name = "Confirm Password")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var normalizedPhone = NormalizePhone(Input.PhoneNumber);
            if (string.IsNullOrWhiteSpace(normalizedPhone))
            {
                ModelState.AddModelError(nameof(Input.PhoneNumber), "Phone number is required.");
                return Page();
            }

            var user = await FindUserByPhoneAsync(normalizedPhone);
            if (user == null)
            {
                // Don't reveal whether the account exists.
                return RedirectToPage("./ForgotPasswordConfirmation");
            }

            var otpCode = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
            var requestId = Guid.NewGuid().ToString("N");
            var expiresUtc = DateTime.UtcNow.Add(OtpLifetime);

            var resetRequest = new PendingPasswordResetRequest
            {
                RequestId = requestId,
                UserId = user.Id,
                PhoneNumber = normalizedPhone,
                NewPassword = Input.Password,
                OtpCode = otpCode,
                ExpiresUtc = expiresUtc
            };

            var cacheKey = PendingPasswordResetRequest.BuildCacheKey(requestId);
            _memoryCache.Set(cacheKey, resetRequest, expiresUtc);

            user.ResetPasswordToken = otpCode;
            user.ResetPasswordTokenExpiry = expiresUtc;
            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                _memoryCache.Remove(cacheKey);
                _logger.LogWarning(
                    "Failed to persist reset OTP state for user {UserId}: {Errors}",
                    user.Id,
                    string.Join("; ", updateResult.Errors.Select(e => e.Description)));

                ModelState.AddModelError(string.Empty, "Unable to start password reset. Please try again.");
                return Page();
            }

            var sent = await _telegramNotifier.SendPasswordOtpAsync(
                normalizedPhone,
                otpCode,
                expiresUtc);
            if (!sent)
            {
                _memoryCache.Remove(cacheKey);
                user.ResetPasswordToken = null;
                user.ResetPasswordTokenExpiry = null;
                await _userManager.UpdateAsync(user);

                ModelState.AddModelError(string.Empty, "Failed to send OTP to Telegram. Please try again.");
                return Page();
            }

            return RedirectToPage("./ResetPassword", new { requestId });
        }

        private async Task<ApplicationUser> FindUserByPhoneAsync(string normalizedPhone)
        {
            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == normalizedPhone);
            if (user != null)
            {
                return user;
            }

            var phoneCandidates = await _userManager.Users
                .Where(u => u.PhoneNumber != null)
                .ToListAsync();

            return phoneCandidates.FirstOrDefault(u => NormalizePhone(u.PhoneNumber) == normalizedPhone);
        }

        private static string NormalizePhone(string phoneNumber)
        {
            return new string((phoneNumber ?? string.Empty).Where(char.IsDigit).ToArray());
        }
    }
}
