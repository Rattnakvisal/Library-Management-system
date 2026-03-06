using System.Net.Http.Headers;
using Library_Management_system.Models;
using Microsoft.Extensions.Options;

namespace Library_Management_system.Services;

public sealed class ContactTelegramNotifier : IContactTelegramNotifier
{
    private readonly HttpClient _httpClient;
    private readonly TelegramContactBotOptions _options;
    private readonly ILogger<ContactTelegramNotifier> _logger;

    public ContactTelegramNotifier(
        HttpClient httpClient,
        IOptions<TelegramContactBotOptions> options,
        ILogger<ContactTelegramNotifier> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<bool> SendAdminAlertAsync(string message, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled ||
            string.IsNullOrWhiteSpace(_options.BotToken) ||
            string.IsNullOrWhiteSpace(_options.AdminChatId) ||
            string.IsNullOrWhiteSpace(message))
        {
            return false;
        }

        var endpoint = $"https://api.telegram.org/bot{_options.BotToken}/sendMessage";
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["chat_id"] = _options.AdminChatId.Trim(),
                ["text"] = message.Trim()
            })
        };

        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        try
        {
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return true;
            }

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning(
                "Contact Telegram notification failed with status {StatusCode}. Response: {Body}",
                (int)response.StatusCode,
                body);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Contact Telegram notification threw an exception.");
            return false;
        }
    }

    public string BuildWelcomeUrl()
    {
        if (string.IsNullOrWhiteSpace(_options.BotUsername))
        {
            return "https://t.me/";
        }

        var userName = _options.BotUsername.Trim().TrimStart('@');
        var payloadSource = string.IsNullOrWhiteSpace(_options.StartPayload)
            ? _options.WelcomePayload
            : _options.StartPayload;
        var payload = string.IsNullOrWhiteSpace(payloadSource)
            ? "welcome_question"
            : payloadSource.Trim();

        return $"https://t.me/{userName}?start={Uri.EscapeDataString(payload)}";
    }
}
