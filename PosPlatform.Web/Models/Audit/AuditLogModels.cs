namespace PosPlatform.Web.Models.Audit
{
    public class AuditLogListItemViewModel
    {
        public int Id { get; set; }

        public DateTime CreatedAt { get; set; }

        public string UserName { get; set; } = "System";
        public string BranchName { get; set; } = "-";

        public string Module { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;

        public string EntityName { get; set; } = string.Empty;
        public int? EntityId { get; set; }

        public string Summary { get; set; } = string.Empty;

        public string? OldValues { get; set; }
        public string? NewValues { get; set; }

        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
    }
}