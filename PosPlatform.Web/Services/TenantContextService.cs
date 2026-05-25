using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace PosPlatform.Web.Services
{
    public class TenantContextService
    {
        private readonly AuthenticationStateProvider _authStateProvider;

        public TenantContextService(AuthenticationStateProvider authStateProvider)
        {
            _authStateProvider = authStateProvider;
        }

        public async Task<int?> GetTenantIdAsync()
        {
            var authState = await _authStateProvider.GetAuthenticationStateAsync();
            var value = authState.User.FindFirst("tenant_id")?.Value;

            return int.TryParse(value, out var tenantId) ? tenantId : null;
        }

        public async Task<int?> GetBranchIdAsync()
        {
            var authState = await _authStateProvider.GetAuthenticationStateAsync();
            var value = authState.User.FindFirst("branch_id")?.Value;

            return int.TryParse(value, out var branchId) ? branchId : null;
        }

        public async Task<ClaimsPrincipal> GetUserAsync()
        {
            var authState = await _authStateProvider.GetAuthenticationStateAsync();
            return authState.User;
        }
    }
}