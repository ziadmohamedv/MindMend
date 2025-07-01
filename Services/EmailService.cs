using Microsoft.Extensions.Configuration;
using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace Mind_Mend.Services
{
    public interface IEmailService
    {
        Task SendEmail(string receptor, string subject, string body);
        Task SendEmailAsync(string email, string subject, string message);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public Task SendEmail(string receptor, string subject, string body)
        {
            throw new NotImplementedException();
        }

        public async Task SendEmailAsync(string email, string subject, string message)
        {
            var emailSettings = _configuration.GetSection("EmailSettings");
            var smtpServer = emailSettings["SmtpServer"] ?? throw new InvalidOperationException("SMTP Server not configured");
            var smtpPort = int.Parse(emailSettings["SmtpPort"] ?? "587");
            var smtpUsername = emailSettings["SmtpUsername"] ?? throw new InvalidOperationException("SMTP Username not configured");
            var smtpPassword = emailSettings["SmtpPassword"] ?? throw new InvalidOperationException("SMTP Password not configured");
            var senderEmail = emailSettings["SenderEmail"] ?? throw new InvalidOperationException("Sender Email not configured");
            var senderName = emailSettings["SenderName"] ?? "Mind-Mend";

            var mailMessage = new MailMessage
            {
                From = new MailAddress(senderEmail, senderName),
                Subject = subject,
                Body = message,
                IsBodyHtml = true
            };
            mailMessage.To.Add(email);

            using (var client = new SmtpClient(smtpServer, smtpPort))
            {
                client.UseDefaultCredentials = false;
                client.Credentials = new NetworkCredential(smtpUsername, smtpPassword);
                client.EnableSsl = true;

                try
                {
                    await client.SendMailAsync(mailMessage);
                    Console.WriteLine($"Email sent successfully to {email}");
                }
                catch (Exception ex)
                {
                    // Log the exception but don't throw it to prevent registration failure
                    Console.WriteLine($"Error sending email: {ex.Message}");
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                    
                    // For debugging Gmail SMTP issues
                    if (ex.InnerException != null)
                    {
                        Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                    }
                    
                    // Don't throw the exception to allow registration to complete
                    // In a production environment, you might want to log this to a file or monitoring service
                }
            }
        }
    }
}
