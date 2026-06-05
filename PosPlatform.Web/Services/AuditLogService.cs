using Microsoft.EntityFrameworkCore;
using PosPlatform.Domain.Entities;
using PosPlatform.Infrastructure.Data;
using PosPlatform.Web.Models.Audit;
using System.Security.Claims;
using System.Text.Json;

namespace PosPlatform.Web.Services
{
    public class AuditLogService
    {
        private readonly AppDbContext _db;
        private readonly TenantContextService _tenantContext;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuditLogService(
            AppDbContext db,
            TenantContextService tenantContext,
            IHttpContextAccessor httpContextAccessor)
        {
            _db = db;
            _tenantContext = tenantContext;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task LogAsync(
            string module,
            string action,
            string entityName,
            int? entityId,
            string summary,
            object? oldValues = null,
            object? newValues = null)
        {
            var tenantId = await _tenantContext.GetTenantIdAsync();

            if (tenantId == null)
            {
                return;
            }

            var branchId = await _tenantContext.GetBranchIdAsync();
            var userId = GetCurrentUserId();
            var userName = GetCurrentUserDisplayName();

            var http = _httpContextAccessor.HttpContext;

            var log = new AuditLog
            {
                TenantId = tenantId.Value,
                BranchId = branchId,
                UserId = userId,
                UserName = userName,

                Module = Clean(module, 80),
                Action = Clean(action, 80),
                EntityName = Clean(entityName, 120),
                EntityId = entityId,
                Summary = Clean(summary, 500),

                OldValues = SerializeValues(oldValues),
                NewValues = SerializeValues(newValues),

                IpAddress = http?.Connection.RemoteIpAddress?.ToString(),
                UserAgent = http?.Request.Headers.UserAgent.ToString(),

                CreatedAt = DateTime.UtcNow
            };

            _db.AuditLogs.Add(log);
            await _db.SaveChangesAsync();
        }

        public async Task<List<AuditLogListItemViewModel>> GetLogsAsync(
            DateTime? fromDate,
            DateTime? toDate,
            string? module,
            string? action,
            string? search)
        {
            var tenantId = await _tenantContext.GetTenantIdAsync();

            if (tenantId == null)
            {
                return new List<AuditLogListItemViewModel>();
            }

            var query = _db.AuditLogs
                .AsNoTracking()
                .Include(x => x.Branch)
                .Where(x => x.TenantId == tenantId.Value);

            if (fromDate.HasValue)
            {
                query = query.Where(x => x.CreatedAt >= fromDate.Value.Date);
            }

            if (toDate.HasValue)
            {
                var to = toDate.Value.Date.AddDays(1);
                query = query.Where(x => x.CreatedAt < to);
            }

            if (!string.IsNullOrWhiteSpace(module) && module != "all")
            {
                query = query.Where(x => x.Module == module);
            }

            if (!string.IsNullOrWhiteSpace(action) && action != "all")
            {
                query = query.Where(x => x.Action == action);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();

                query = query.Where(x =>
                    x.UserName.Contains(term) ||
                    x.Module.Contains(term) ||
                    x.Action.Contains(term) ||
                    x.EntityName.Contains(term) ||
                    x.Summary.Contains(term));
            }

            return await query
                .OrderByDescending(x => x.CreatedAt)
                .Take(250)
                .Select(x => new AuditLogListItemViewModel
                {
                    Id = x.Id,
                    CreatedAt = x.CreatedAt,
                    UserName = x.UserName,
                    BranchName = x.Branch != null ? x.Branch.Name : "-",
                    Module = x.Module,
                    Action = x.Action,
                    EntityName = x.EntityName,
                    EntityId = x.EntityId,
                    Summary = x.Summary,
                    OldValues = x.OldValues,
                    NewValues = x.NewValues,
                    IpAddress = x.IpAddress,
                    UserAgent = x.UserAgent
                })
                .ToListAsync();
        }

        private int? GetCurrentUserId()
        {
            var value = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(value, out var id) ? id : null;
        }

        private string GetCurrentUserDisplayName()
        {
            var user = _httpContextAccessor.HttpContext?.User;

            return user?.FindFirstValue(ClaimTypes.Name)
                ?? user?.Identity?.Name
                ?? user?.FindFirstValue(ClaimTypes.Email)
                ?? "System";
        }

        private static string Clean(string value, int maxLength)
        {
            var cleaned = string.IsNullOrWhiteSpace(value) ? "-" : value.Trim();

            return cleaned.Length <= maxLength
                ? cleaned
                : cleaned.Substring(0, maxLength);
        }

        private static string? SerializeValues(object? value)
        {
            if (value == null)
            {
                return null;
            }

            return JsonSerializer.Serialize(value, new JsonSerializerOptions
            {
                WriteIndented = true
            });
        }
    }
}