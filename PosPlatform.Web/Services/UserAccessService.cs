using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using PosPlatform.Infrastructure.Data;
using System.Security.Claims;

namespace PosPlatform.Web.Services
{
    public class UserAccessService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly AuthenticationStateProvider _authStateProvider;

        public UserAccessService(
            IServiceScopeFactory scopeFactory,
            AuthenticationStateProvider authStateProvider)
        {
            _scopeFactory = scopeFactory;
            _authStateProvider = authStateProvider;
        }

        public async Task<string> GetCurrentRoleAsync()
        {
            var authState = await _authStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;

            var userIdValue = user.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdValue, out var userId))
            {
                return "Viewer";
            }

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var roleName = await db.UserRoles
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .Select(x => x.Role != null ? x.Role.Name : null)
                .FirstOrDefaultAsync();

            return string.IsNullOrWhiteSpace(roleName) ? "Viewer" : roleName;
        }

        public async Task<bool> CanAccessAsync(string module)
        {
            var role = await GetCurrentRoleAsync();

            if (role is "Owner" or "Admin")
            {
                return true;
            }

            return module.ToLowerInvariant() switch
            {
                "dashboard" => true,

                "products" => role is "Inventory User" or "Manager",
                "stock" => role is "Inventory User" or "Manager",
                "stock-purchases" => role is "Manager" or "Inventory User" or "Accounting User",
                "suppliers" => role is "Manager" or "Inventory User" or "Accounting User",

                "sales" => role is "Sales User" or "Manager",
                "cashier-shift" => role is "Sales User" or "Manager",
                "customers" => role is "Sales User" or "Manager" or "Accounting User",

                "branches" => role is "Manager" or "Accounting User",
                "reports" => role is "Accounting User" or "Manager" or "Viewer",
                "expenses" => role is "Manager" or "Accounting User",

                "stock-transfers" => role is "Manager" or "Inventory User",
                "barcode-labels" => role is "Manager" or "Inventory User",
                "audit-trail" => role is "Owner" or "Admin" or "Manager",
                "quotes" => role is "Sales User" or "Manager" or "Accounting User",
                "invoices" => role is "Sales User" or "Manager" or "Accounting User",
                "customer-statements" => role is "Sales User" or "Manager" or "Accounting User",
                "overdue-invoices" => role is "Sales User" or "Manager" or "Accounting User",
                "invoice-follow-ups" => role is "Sales User" or "Manager" or "Accounting User",
                "product-variants" => role is "Inventory User" or "Manager",
                "users" => false,
                "settings" => false,

                _ => false
            };
        }
    }
}