using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Identity.UI.Services;
using MimeKit;

namespace Library_Management_system.Services;

public class EmailSender : IEmailSender
{
    private readonly IConfiguration _configuration;

    public EmailSender(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        var emailSettings = _configuration.GetSection("EmailSettings");

        var message = new MimeMessage();
        // Uses the SenderName and SenderEmail from your appsettings.json
        message.From.Add(new MailboxAddress(
            emailSettings["SenderName"] ?? "Library System",
            emailSettings["SenderEmail"]));

        message.To.Add(MailboxAddress.Parse(email));
        message.Subject = subject;

        var builder = new BodyBuilder { HtmlBody = htmlMessage };
        message.Body = builder.ToMessageBody();

        using var smtp = new SmtpClient();
        try
        {
            await smtp.ConnectAsync(
                emailSettings["SmtpServer"],
                int.Parse(emailSettings["SmtpPort"] ?? "587"),
                SecureSocketOptions.StartTls);

            // IMPORTANT: Use your Google App Password here, not your regular Gmail password
            await smtp.AuthenticateAsync(
                emailSettings["SenderEmail"],
                emailSettings["Password"]);

            await smtp.SendAsync(message);
            await smtp.DisconnectAsync(true);
        }
        finally
        {
            await smtp.DisconnectAsync(true);
        }
    }
}