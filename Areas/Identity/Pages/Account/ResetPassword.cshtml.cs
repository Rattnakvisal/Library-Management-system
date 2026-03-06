// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
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

namespace Library_Management_system.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class ResetPasswordModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMemoryCache _memoryCache;

        public ResetPasswordModel(UserManager<ApplicationUser> userManager, IMemoryCache memoryCache)
        {
            _userManager = userManager;
            _memoryCache = memoryCache;
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
            if (!TryGetPendingRequest(requestId, out var resetRequest))
            {
                TempData["ErrorMessage"] = "Reset request is invalid or expired. Please try again.";
                return RedirectToPage("./ForgotPassword");
            }

            Input = new InputModel
            {
                RequestId = resetRequest.RequestId
            };

            PhoneNumberMask = MaskPhoneNumber(resetRequest.PhoneNumber);
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

            if (!TryGetPendingRequest(Input.RequestId, out var resetRequest))
            {
                ModelState.AddModelError(string.Empty, "OTP has expired. Please request a new code.");
                return Page();
            }

            PhoneNumberMask = MaskPhoneNumber(resetRequest.PhoneNumber);

            if (!string.Equals(resetRequest.OtpCode, Input.OtpCode?.Trim(), StringComparison.Ordinal))
            {
                ModelState.AddModelError(nameof(Input.OtpCode), "OTP code is incorrect.");
                return Page();
            }

            var user = await _userManager.FindByIdAsync(resetRequest.UserId);
            if (user == null)
            {
                _memoryCache.Remove(PendingPasswordResetRequest.BuildCacheKey(resetRequest.RequestId));
                return RedirectToPage("./ResetPasswordConfirmation");
            }

            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, resetToken, resetRequest.NewPassword);
            if (result.Succeeded)
            {
                user.ResetPasswordToken = null;
                user.ResetPasswordTokenExpiry = null;
                await _userManager.UpdateAsync(user);

                _memoryCache.Remove(PendingPasswordResetRequest.BuildCacheKey(resetRequest.RequestId));
                return RedirectToPage("./ResetPasswordConfirmation");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return Page();
        }

        private bool TryGetPendingRequest(string requestId, out PendingPasswordResetRequest resetRequest)
        {
            resetRequest = null;
            if (string.IsNullOrWhiteSpace(requestId))
            {
                return false;
            }

            var cacheKey = PendingPasswordResetRequest.BuildCacheKey(requestId);
            if (!_memoryCache.TryGetValue(cacheKey, out PendingPasswordResetRequest cached) || cached == null)
            {
                return false;
            }

            if (cached.ExpiresUtc <= DateTime.UtcNow)
            {
                _memoryCache.Remove(cacheKey);
                return false;
            }

            resetRequest = cached;
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
