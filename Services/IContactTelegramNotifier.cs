namespace Library_Management_system.Services;

public interface IContactTelegramNotifier
{
    Task<bool> SendAdminAlertAsync(string message, CancellationToken cancellationToken = default);
    string BuildWelcomeUrl();
}
