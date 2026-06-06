namespace PosPlatform.Web.Models.Email
{
    public class EmailSettings
    {
        public string FromName { get; set; } = "POS Platform";
        public string FromEmail { get; set; } = "noreply@localhost";

        public string SmtpHost { get; set; } = string.Empty;
        public int SmtpPort { get; set; } = 587;

        public string? SmtpUsername { get; set; }
        public string? SmtpPassword { get; set; }

        public bool EnableSsl { get; set; } = true;

        public string AppBaseUrl { get; set; } = "https://localhost:5001";
    }
}