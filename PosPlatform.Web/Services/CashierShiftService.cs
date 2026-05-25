using Microsoft.EntityFrameworkCore;
using PosPlatform.Domain.Entities;
using PosPlatform.Infrastructure.Data;
using PosPlatform.Web.Models.Shifts;
using System.Security.Claims;

namespace PosPlatform.Web.Services
{
    public class CashierShiftService
    {
        private readonly AppDbContext _db;
        private readonly TenantContextService _tenantContext;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CashierShiftService(
            AppDbContext db,
            TenantContextService tenantContext,
            IHttpContextAccessor httpContextAccessor)
        {
            _db = db;
            _tenantContext = tenantContext;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<CashierShiftViewModel?> GetOpenShiftAsync()
        {
            var tenantId = await _tenantContext.GetTenantIdAsync();
            var userId = GetCurrentUserId();

            if (tenantId == null || userId == null)
            {
                return null;
            }

            var shift = await _db.CashierShifts
                .AsNoTracking()
                .Include(x => x.Branch)
                .FirstOrDefaultAsync(x =>
                    x.TenantId == tenantId.Value &&
                    x.CashierUserId == userId.Value &&
                    x.Status == "Open");

            if (shift == null)
            {
                return null;
            }

            return await BuildShiftViewModelAsync(shift);
        }

        public async Task<List<CashierShiftViewModel>> GetMyShiftsAsync(DateTime? fromDate, DateTime? toDate)
        {
            var tenantId = await _tenantContext.GetTenantIdAsync();
            var userId = GetCurrentUserId();

            if (tenantId == null || userId == null)
            {
                return new List<CashierShiftViewModel>();
            }

            var query = _db.CashierShifts
                .AsNoTracking()
                .Include(x => x.Branch)
                .Where(x =>
                    x.TenantId == tenantId.Value &&
                    x.CashierUserId == userId.Value);

            if (fromDate.HasValue)
            {
                query = query.Where(x => x.OpenedAt >= fromDate.Value.Date);
            }

            if (toDate.HasValue)
            {
                var to = toDate.Value.Date.AddDays(1);
                query = query.Where(x => x.OpenedAt < to);
            }

            var shifts = await query
                .OrderByDescending(x => x.OpenedAt)
                .Take(100)
                .ToListAsync();

            var result = new List<CashierShiftViewModel>();

            foreach (var shift in shifts)
            {
                result.Add(await BuildShiftViewModelAsync(shift));
            }

            return result;
        }

        public async Task<(bool Success, string Message)> OpenShiftAsync(OpenShiftModel model)
        {
            var tenantId = await _tenantContext.GetTenantIdAsync();
            var branchId = await _tenantContext.GetBranchIdAsync();
            var userId = GetCurrentUserId();
            var cashierName = GetCurrentUserDisplayName();

            if (tenantId == null || userId == null)
            {
                return (false, "Logged-in user could not be identified.");
            }

            var hasOpenShift = await _db.CashierShifts.AnyAsync(x =>
                x.TenantId == tenantId.Value &&
                x.CashierUserId == userId.Value &&
                x.Status == "Open");

            if (hasOpenShift)
            {
                return (false, "You already have an open shift.");
            }

            _db.CashierShifts.Add(new CashierShift
            {
                TenantId = tenantId.Value,
                BranchId = branchId,
                CashierUserId = userId.Value,
                CashierName = cashierName,
                OpeningCash = model.OpeningCash,
                OpeningNotes = Clean(model.OpeningNotes),
                Status = "Open",
                OpenedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();

            return (true, "Shift opened successfully.");
        }

        public async Task<(bool Success, string Message)> CloseShiftAsync(CloseShiftModel model)
        {
            var tenantId = await _tenantContext.GetTenantIdAsync();
            var userId = GetCurrentUserId();

            if (tenantId == null || userId == null)
            {
                return (false, "Logged-in user could not be identified.");
            }

            var shift = await _db.CashierShifts.FirstOrDefaultAsync(x =>
                x.TenantId == tenantId.Value &&
                x.CashierUserId == userId.Value &&
                x.Status == "Open");

            if (shift == null)
            {
                return (false, "You do not have an open shift.");
            }

            var totals = await CalculateShiftTotalsAsync(
                tenantId.Value,
                userId.Value,
                shift.OpenedAt,
                DateTime.UtcNow);

            shift.CashSales = totals.CashSales;
            shift.CardSales = totals.CardSales;
            shift.EftSales = totals.EftSales;
            shift.TotalSales = totals.TotalSales;

            shift.ClosingCash = model.ClosingCash;
            shift.ExpectedCash = shift.OpeningCash + totals.CashSales;
            shift.CashDifference = model.ClosingCash - shift.ExpectedCash;

            shift.ClosingNotes = Clean(model.ClosingNotes);
            shift.Status = "Closed";
            shift.ClosedAt = DateTime.UtcNow;
            shift.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return (true, "Shift closed successfully.");
        }

        private async Task<CashierShiftViewModel> BuildShiftViewModelAsync(CashierShift shift)
        {
            var endDate = shift.ClosedAt ?? DateTime.UtcNow;

            var totals = shift.Status == "Open"
                ? await CalculateShiftTotalsAsync(shift.TenantId, shift.CashierUserId, shift.OpenedAt, endDate)
                : new ShiftTotals
                {
                    CashSales = shift.CashSales,
                    CardSales = shift.CardSales,
                    EftSales = shift.EftSales,
                    TotalSales = shift.TotalSales
                };

            var expectedCash = shift.Status == "Open"
                ? shift.OpeningCash + totals.CashSales
                : shift.ExpectedCash;

            var cashDifference = shift.Status == "Open"
                ? 0
                : shift.CashDifference;

            return new CashierShiftViewModel
            {
                Id = shift.Id,
                CashierName = shift.CashierName,
                BranchName = shift.Branch?.Name ?? "-",
                OpenedAt = shift.OpenedAt,
                ClosedAt = shift.ClosedAt,
                OpeningCash = shift.OpeningCash,
                ClosingCash = shift.ClosingCash,
                CashSales = totals.CashSales,
                CardSales = totals.CardSales,
                EftSales = totals.EftSales,
                TotalSales = totals.TotalSales,
                ExpectedCash = expectedCash,
                CashDifference = cashDifference,
                Status = shift.Status,
                OpeningNotes = shift.OpeningNotes,
                ClosingNotes = shift.ClosingNotes
            };
        }

        private async Task<ShiftTotals> CalculateShiftTotalsAsync(
            int tenantId,
            int cashierUserId,
            DateTime from,
            DateTime to)
        {
            var sales = await _db.Sales
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId &&
                    x.CashierUserId == cashierUserId &&
                    x.Status == "Completed" &&
                    x.CreatedAt >= from &&
                    x.CreatedAt <= to)
                .Select(x => new
                {
                    x.PaymentMethod,
                    x.TotalAmount
                })
                .ToListAsync();

            return new ShiftTotals
            {
                CashSales = sales
                    .Where(x => x.PaymentMethod.Equals("Cash", StringComparison.OrdinalIgnoreCase))
                    .Sum(x => x.TotalAmount),

                CardSales = sales
                    .Where(x => x.PaymentMethod.Equals("Card", StringComparison.OrdinalIgnoreCase))
                    .Sum(x => x.TotalAmount),

                EftSales = sales
                    .Where(x => x.PaymentMethod.Equals("EFT", StringComparison.OrdinalIgnoreCase))
                    .Sum(x => x.TotalAmount),

                TotalSales = sales.Sum(x => x.TotalAmount)
            };
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
                ?? "Cashier";
        }

        private static string? Clean(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private class ShiftTotals
        {
            public decimal CashSales { get; set; }
            public decimal CardSales { get; set; }
            public decimal EftSales { get; set; }
            public decimal TotalSales { get; set; }
        }
    }
}