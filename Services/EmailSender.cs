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
        var senderEmail = emailSettings["SenderEmail"]
            ?? throw new InvalidOperationException("EmailSettings:SenderEmail is required.");
        var smtpServer = emailSettings["SmtpServer"]
            ?? throw new InvalidOperationException("EmailSettings:SmtpServer is required.");
        var password = emailSettings["Password"]
            ?? throw new InvalidOperationException("EmailSettings:Password is required.");

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(
            emailSettings["SenderName"] ?? "Library System",
            senderEmail));

        message.To.Add(MailboxAddress.Parse(email));
        message.Subject = subject;

        var builder = new BodyBuilder { HtmlBody = htmlMessage };
        message.Body = builder.ToMessageBody();

        using var smtp = new SmtpClient();
        try
        {
            await smtp.ConnectAsync(
                smtpServer,
                int.Parse(emailSettings["SmtpPort"] ?? "587"),
                SecureSocketOptions.StartTls);

            await smtp.AuthenticateAsync(
                senderEmail,
                password);

            await smtp.SendAsync(message);
            await smtp.DisconnectAsync(true);
        }
        finally
        {
            await smtp.DisconnectAsync(true);
        }
    }
}
