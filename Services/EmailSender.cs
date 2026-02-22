using Microsoft.AspNetCore.Identity.UI.Services;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace Library_Management_system.Services
{
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
            message.From.Add(new MailboxAddress(
                emailSettings["SenderName"],
                emailSettings["SenderEmail"]));
            message.To.Add(MailboxAddress.Parse(email));
            message.Subject = subject;

            var builder = new BodyBuilder { HtmlBody = htmlMessage };
            message.Body = builder.ToMessageBody();

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(
                emailSettings["SmtpServer"],
                int.Parse(emailSettings["SmtpPort"]!),
                SecureSocketOptions.StartTls);

            await smtp.AuthenticateAsync(
                emailSettings["Username"],
                emailSettings["Password"]);

            await smtp.SendAsync(message);
            await smtp.DisconnectAsync(true);
        }
    }
}
