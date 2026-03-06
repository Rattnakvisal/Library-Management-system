namespace Library_Management_system.Models;

public sealed class TelegramBotOptions
{
    public const string SectionName = "TelegramBot";

    public bool Enabled { get; set; }
    public string BotToken { get; set; } = string.Empty;
    public string AdminChatId { get; set; } = string.Empty;
}
