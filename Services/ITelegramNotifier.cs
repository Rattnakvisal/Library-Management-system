using System;

namespace Library_Management_system.Services;

public interface ITelegramNotifier
{
    Task<bool> SendAdminAlertAsync(string message, CancellationToken cancellationToken = default);
    Task<bool> SendPasswordOtpAsync(
        string phoneNumber,
        string otpCode,
        DateTime expiresUtc,
        CancellationToken cancellationToken = default);
}
