using System;

namespace Library_Management_system.Areas.Identity.Pages.Account;

public sealed class PendingLoginOtpRequest
{
    public string RequestId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string OtpCode { get; set; } = string.Empty;
    public bool RememberMe { get; set; }
    public string ReturnUrl { get; set; } = string.Empty;
    public DateTime ExpiresUtc { get; set; }

    public static string BuildCacheKey(string requestId) => $"login-otp:{requestId.Trim()}";
}
