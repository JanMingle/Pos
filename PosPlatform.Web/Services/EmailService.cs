using Microsoft.Extensions.Options;
using PosPlatform.Web.Models.Email;
using System.Net;
using System.Net.Mail;

namespace PosPlatform.Web.Services
{
    public class EmailService
    {
        private readonly EmailSettings _settings;

        public EmailService(IOptions<EmailSettings> options)
        {
            _settings = options.Value;
        }

        public async Task<(bool Success, string Message)> SendEmailAsync(
            string toEmail,
            string subject,
            string htmlBody)
        {
            if (string.IsNullOrWhiteSpace(toEmail))
            {
                return (false, "Recipient email is required.");
            }

            if (string.IsNullOrWhiteSpace(_settings.SmtpHost))
            {
                return (false, "SMTP host is not configured in appsettings.json.");
            }

            if (string.IsNullOrWhiteSpace(_settings.FromEmail))
            {
                return (false, "Sender email is not configured in appsettings.json.");
            }

            try
            {
                using var message = new MailMessage();

                message.From = new MailAddress(_settings.FromEmail, _settings.FromName);
                message.To.Add(new MailAddress(toEmail.Trim()));
                message.Subject = subject;
                message.Body = htmlBody;
                message.IsBodyHtml = true;

                using var client = new SmtpClient(_settings.SmtpHost, _settings.SmtpPort)
                {
                    EnableSsl = _settings.EnableSsl,
                    DeliveryMethod = SmtpDeliveryMethod.Network
                };

                if (!string.IsNullOrWhiteSpace(_settings.SmtpUsername))
                {
                    client.Credentials = new NetworkCredential(
                        _settings.SmtpUsername,
                        _settings.SmtpPassword
                    );
                }

                await client.SendMailAsync(message);

                return (true, "Email sent successfully.");
            }
            catch (Exception ex)
            {
                return (false, $"Email failed: {ex.Message}");
            }
        }

        public string GetAppBaseUrl()
        {
            return string.IsNullOrWhiteSpace(_settings.AppBaseUrl)
                ? "https://localhost:5001"
                : _settings.AppBaseUrl.TrimEnd('/');
        }
    }
}