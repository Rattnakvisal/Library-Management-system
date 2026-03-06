namespace Library_Management_system.Models;

public sealed class TelegramContactBotOptions
{
    public const string SectionName = "TelegramContactBot";

    public bool Enabled { get; set; }
    public string BotToken { get; set; } = string.Empty;
    public string AdminChatId { get; set; } = string.Empty;
    public string BotUsername { get; set; } = string.Empty;
    public string StartPayload { get; set; } = "welcome_question";
    public string WelcomePayload { get; set; } = "welcome";
}
