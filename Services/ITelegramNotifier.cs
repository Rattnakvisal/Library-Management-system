namespace Library_Management_system.Services;

public interface ITelegramNotifier
{
    Task<bool> SendAdminAlertAsync(string message, CancellationToken cancellationToken = default);
}
