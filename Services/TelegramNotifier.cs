using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using Library_Management_system.Models;
using Microsoft.Extensions.Options;

namespace Library_Management_system.Services;

public sealed class TelegramNotifier : ITelegramNotifier
{
    private readonly HttpClient _httpClient;
    private readonly TelegramBotOptions _options;
    private readonly ILogger<TelegramNotifier> _logger;

    public TelegramNotifier(
        HttpClient httpClient,
        IOptions<TelegramBotOptions> options,
        ILogger<TelegramNotifier> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<bool> SendAdminAlertAsync(string message, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled || string.IsNullOrWhiteSpace(message))
        {
            return false;
        }

        return await SendMessageAsync(
            _options.BotToken,
            _options.AdminChatId,
            message,
            cancellationToken);
    }

    public async Task<bool> SendPasswordOtpAsync(
        string phoneNumber,
        string otpCode,
        DateTime expiresUtc,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled ||
            string.IsNullOrWhiteSpace(phoneNumber) ||
            string.IsNullOrWhiteSpace(otpCode))
        {
            return false;
        }

        var chatId = string.IsNullOrWhiteSpace(_options.OtpChatId)
            ? _options.AdminChatId
            : _options.OtpChatId;

        return await SendPasswordOtpToChatAsync(chatId, phoneNumber, otpCode, expiresUtc, cancellationToken);
    }

    public async Task<bool> SendPasswordOtpToChatAsync(
        string chatId,
        string phoneNumber,
        string otpCode,
        DateTime expiresUtc,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled ||
            string.IsNullOrWhiteSpace(chatId) ||
            string.IsNullOrWhiteSpace(phoneNumber) ||
            string.IsNullOrWhiteSpace(otpCode))
        {
            return false;
        }

        var expiresKh = DateTime.SpecifyKind(expiresUtc, DateTimeKind.Utc).ToUniversalTime().AddHours(7);

        var message = string.Join('\n',
            "AUB Library password reset OTP.",
            $"Phone: {phoneNumber.Trim()}",
            $"OTP: {otpCode.Trim()}",
            $"Expires (Cambodia UTC+7): {expiresKh:yyyy-MM-dd HH:mm:ss}",
            $"Expires (UTC): {expiresUtc:yyyy-MM-dd HH:mm:ss}");

        foreach (var botToken in GetLookupBotTokens())
        {
            var sent = await SendMessageAsync(botToken, chatId, message, cancellationToken);
            if (sent)
            {
                return true;
            }
        }

        return false;
    }

    public async Task<string?> FindUserChatIdByPhoneAsync(string phoneNumber, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled || string.IsNullOrWhiteSpace(phoneNumber))
        {
            return null;
        }

        var targetCandidates = PhoneNumberHelper.BuildMatchCandidates(phoneNumber);
        if (targetCandidates.Count == 0)
        {
            return null;
        }

        var startOnlyChatCandidates = new HashSet<string>(StringComparer.Ordinal);

        foreach (var botToken in GetLookupBotTokens())
        {
            var updates = await GetRecentUpdatesAsync(botToken, cancellationToken);
            foreach (var update in updates.OrderByDescending(u => u.UpdateId))
            {
                if (update.Message is null ||
                    string.IsNullOrWhiteSpace(update.Message.ChatId) ||
                    !string.Equals(update.Message.ChatType, "private", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var messageCandidates = BuildPhoneCandidatesFromMessage(update.Message);
                if (!messageCandidates.Overlaps(targetCandidates))
                {
                    if (LooksLikeRecentStartWithoutPhone(update.Message))
                    {
                        startOnlyChatCandidates.Add(update.Message.ChatId);
                    }

                    continue;
                }

                return update.Message.ChatId;
            }
        }

        // Fallback: if exactly one private user recently clicked /start,
        // bind to that chat for the current reset attempt.
        if (startOnlyChatCandidates.Count == 1)
        {
            return startOnlyChatCandidates.First();
        }

        return null;
    }

    private async Task<bool> SendMessageAsync(
        string botToken,
        string chatId,
        string message,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(botToken) ||
            string.IsNullOrWhiteSpace(chatId) ||
            string.IsNullOrWhiteSpace(message))
        {
            return false;
        }

        var endpoint = $"https://api.telegram.org/bot{botToken.Trim()}/sendMessage";
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["chat_id"] = chatId.Trim(),
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
                "Telegram notification failed with status {StatusCode}. Response: {Body}",
                (int)response.StatusCode,
                body);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Telegram notification threw an exception.");
            return false;
        }
    }

    private async Task<IReadOnlyList<TelegramUpdate>> GetRecentUpdatesAsync(string botToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(botToken))
        {
            return Array.Empty<TelegramUpdate>();
        }

        // Read the latest updates window first so recent /start + phone messages are not missed.
        var latestWindow = await GetUpdatesBatchAsync(botToken, "-100", cancellationToken);
        if (latestWindow.Count > 0)
        {
            return latestWindow;
        }

        return await GetUpdatesBatchAsync(botToken, null, cancellationToken);
    }

    private async Task<IReadOnlyList<TelegramUpdate>> GetUpdatesBatchAsync(
        string botToken,
        string? offset,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(botToken))
        {
            return Array.Empty<TelegramUpdate>();
        }

        var endpoint = $"https://api.telegram.org/bot{botToken.Trim()}/getUpdates?limit=100&timeout=0&allowed_updates=%5B%22message%22%5D";
        if (!string.IsNullOrWhiteSpace(offset))
        {
            endpoint = $"{endpoint}&offset={offset.Trim()}";
        }

        try
        {
            using var response = await _httpClient.GetAsync(endpoint, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning(
                    "Telegram getUpdates failed with status {StatusCode}. Response: {Body}",
                    (int)response.StatusCode,
                    body);
                return Array.Empty<TelegramUpdate>();
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            using var document = JsonDocument.Parse(content);
            var root = document.RootElement;

            if (!root.TryGetProperty("ok", out var okElement) || !okElement.GetBoolean())
            {
                return Array.Empty<TelegramUpdate>();
            }

            if (!root.TryGetProperty("result", out var resultElement) || resultElement.ValueKind != JsonValueKind.Array)
            {
                return Array.Empty<TelegramUpdate>();
            }

            var updates = new List<TelegramUpdate>();
            foreach (var item in resultElement.EnumerateArray())
            {
                if (!item.TryGetProperty("update_id", out var updateIdElement) ||
                    updateIdElement.ValueKind != JsonValueKind.Number ||
                    !updateIdElement.TryGetInt64(out var updateId))
                {
                    continue;
                }

                if (!item.TryGetProperty("message", out var messageElement) ||
                    messageElement.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                if (!messageElement.TryGetProperty("chat", out var chatElement) ||
                    chatElement.ValueKind != JsonValueKind.Object ||
                    !chatElement.TryGetProperty("id", out var chatIdElement))
                {
                    continue;
                }

                var chatId = GetJsonStringOrRaw(chatIdElement);
                if (string.IsNullOrWhiteSpace(chatId))
                {
                    continue;
                }

                var chatType = "unknown";
                if (chatElement.TryGetProperty("type", out var chatTypeElement))
                {
                    chatType = GetJsonStringOrRaw(chatTypeElement) ?? "unknown";
                }

                string? text = null;
                if (messageElement.TryGetProperty("text", out var textElement))
                {
                    text = GetJsonStringOrRaw(textElement);
                }

                string? contactPhone = null;
                if (messageElement.TryGetProperty("contact", out var contactElement) &&
                    contactElement.ValueKind == JsonValueKind.Object &&
                    contactElement.TryGetProperty("phone_number", out var phoneElement))
                {
                    contactPhone = GetJsonStringOrRaw(phoneElement);
                }

                DateTime? sentAtUtc = null;
                if (messageElement.TryGetProperty("date", out var dateElement) &&
                    dateElement.ValueKind == JsonValueKind.Number &&
                    dateElement.TryGetInt64(out var unixSeconds))
                {
                    sentAtUtc = DateTimeOffset.FromUnixTimeSeconds(unixSeconds).UtcDateTime;
                }

                updates.Add(new TelegramUpdate(updateId, new TelegramMessage(chatId, chatType, text, contactPhone, sentAtUtc)));
            }

            return updates;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Telegram getUpdates request failed.");
            return Array.Empty<TelegramUpdate>();
        }
    }

    private static HashSet<string> BuildPhoneCandidatesFromMessage(TelegramMessage message)
    {
        var candidates = new HashSet<string>(StringComparer.Ordinal);

        AddPhoneCandidates(candidates, message.ContactPhone);

        if (!string.IsNullOrWhiteSpace(message.Text))
        {
            AddPhoneCandidates(candidates, TryExtractPhoneFromStartCommand(message.Text));
            AddPhoneCandidates(candidates, message.Text);
        }

        return candidates;
    }

    private static string? TryExtractPhoneFromStartCommand(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var trimmed = text.Trim();
        if (!trimmed.StartsWith("/start", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var payload = trimmed.Length <= 6 ? string.Empty : trimmed[6..].Trim();
        if (string.IsNullOrWhiteSpace(payload))
        {
            return null;
        }

        if (payload.StartsWith("phone_", StringComparison.OrdinalIgnoreCase))
        {
            payload = payload[6..];
        }

        return payload;
    }

    private static void AddPhoneCandidates(ISet<string> candidates, string? source)
    {
        if (!PhoneNumberHelper.LooksLikePhone(source))
        {
            return;
        }

        foreach (var candidate in PhoneNumberHelper.BuildMatchCandidates(source))
        {
            candidates.Add(candidate);
        }
    }

    private static bool LooksLikeRecentStartWithoutPhone(TelegramMessage message)
    {
        if (string.IsNullOrWhiteSpace(message.Text) || !IsStartWithoutPayload(message.Text))
        {
            return false;
        }

        if (message.SentAtUtc is null)
        {
            return false;
        }

        var age = DateTime.UtcNow - message.SentAtUtc.Value;
        return age >= TimeSpan.Zero && age <= TimeSpan.FromMinutes(20);
    }

    private static bool IsStartWithoutPayload(string text)
    {
        var trimmed = text.Trim();
        if (!trimmed.StartsWith("/start", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var commandBody = trimmed[6..].Trim();
        return string.IsNullOrWhiteSpace(commandBody);
    }

    private string ResolveOtpBotToken()
    {
        return string.IsNullOrWhiteSpace(_options.OtpBotToken)
            ? _options.BotToken
            : _options.OtpBotToken;
    }

    private IReadOnlyList<string> GetLookupBotTokens()
    {
        var tokens = new List<string>();
        var preferredToken = ResolveOtpBotToken()?.Trim();
        var fallbackToken = (_options.BotToken ?? string.Empty).Trim();

        if (!string.IsNullOrWhiteSpace(preferredToken))
        {
            tokens.Add(preferredToken);
        }

        if (!string.IsNullOrWhiteSpace(fallbackToken) &&
            !tokens.Contains(fallbackToken, StringComparer.Ordinal))
        {
            tokens.Add(fallbackToken);
        }

        return tokens;
    }

    private static string? GetJsonStringOrRaw(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.GetRawText(),
            _ => null
        };
    }

    private sealed record TelegramUpdate(long UpdateId, TelegramMessage? Message);

    private sealed record TelegramMessage(string ChatId, string ChatType, string? Text, string? ContactPhone, DateTime? SentAtUtc);
}
