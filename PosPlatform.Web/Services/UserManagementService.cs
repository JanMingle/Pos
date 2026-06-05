using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PosPlatform.Domain.Entities;
using PosPlatform.Infrastructure.Data;
using PosPlatform.Web.Models.Users;

namespace PosPlatform.Web.Services
{
    public class UserManagementService
    {
        private readonly AppDbContext _db;
        private readonly TenantContextService _tenantContext;
        private readonly IPasswordHasher<ApplicationUser> _passwordHasher;
        private readonly AuditLogService _auditLogService;

        private static readonly List<RoleOptionViewModel> DefaultRoles = new()
        {
            new RoleOptionViewModel
            {
                Name = "Owner",
                Description = "Main business owner with full access to the entire platform."
            },
            new RoleOptionViewModel
            {
                Name = "Admin",
                Description = "Full access to manage users, products, stock, sales and reports."
            },
            new RoleOptionViewModel
            {
                Name = "Sales User",
                Description = "Can process sales and view their own sales activity."
            },
            new RoleOptionViewModel
            {
                Name = "Inventory User",
                Description = "Can manage products, stock levels and stock adjustments."
            },
            new RoleOptionViewModel
            {
                Name = "Accounting User",
                Description = "Can view sales reports, payment totals and financial summaries."
            },
            new RoleOptionViewModel
            {
                Name = "Manager",
                Description = "Can oversee sales, stock and users without full system ownership."
            },
            new RoleOptionViewModel
            {
                Name = "Viewer",
                Description = "Read-only access for monitoring business activity."
            }
        };

        public UserManagementService(
            AppDbContext db,
            TenantContextService tenantContext,
            IPasswordHasher<ApplicationUser> passwordHasher,
            AuditLogService auditLogService)
        {
            _db = db;
            _tenantContext = tenantContext;
            _passwordHasher = passwordHasher;
            _auditLogService = auditLogService;
        }

        public async Task EnsureDefaultRolesAsync()
        {
            var tenantId = await _tenantContext.GetTenantIdAsync();

            if (tenantId == null)
            {
                return;
            }

            foreach (var roleOption in DefaultRoles)
            {
                var exists = await _db.Roles.AnyAsync(x =>
                    x.TenantId == tenantId.Value &&
                    x.Name == roleOption.Name);

                if (!exists)
                {
                    _db.Roles.Add(new Role
                    {
                        TenantId = tenantId.Value,
                        Name = roleOption.Name,
                        NormalizedName = NormalizeRoleName(tenantId.Value, roleOption.Name),
                        Description = roleOption.Description,
                        ConcurrencyStamp = Guid.NewGuid().ToString()
                    });
                }
            }

            await _db.SaveChangesAsync();
        }

        public async Task<List<RoleOptionViewModel>> GetRoleOptionsAsync()
        {
            await EnsureDefaultRolesAsync();
            return DefaultRoles;
        }

        public async Task<List<BranchOptionViewModel>> GetBranchesAsync()
        {
            var tenantId = await _tenantContext.GetTenantIdAsync();

            if (tenantId == null)
            {
                return new List<BranchOptionViewModel>();
            }

            await EnsureMainBranchExistsAsync(tenantId.Value);

            return await _db.Branches
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId.Value && x.IsActive)
                .OrderByDescending(x => x.IsMainBranch)
                .ThenBy(x => x.Name)
                .Select(x => new BranchOptionViewModel
                {
                    Id = x.Id,
                    Name = x.Name,
                    IsMainBranch = x.IsMainBranch
                })
                .ToListAsync();
        }

        public async Task<List<UserListItemViewModel>> GetUsersAsync(string? search, string? roleFilter, int? branchFilter)
        {
            var tenantId = await _tenantContext.GetTenantIdAsync();

            if (tenantId == null)
            {
                return new List<UserListItemViewModel>();
            }

            await EnsureDefaultRolesAsync();

            var query = _db.Users
                .AsNoTracking()
                .Include(x => x.Branch)
                .Include(x => x.UserRoles)
                    .ThenInclude(x => x.Role)
                .Where(x => x.TenantId == tenantId.Value);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();

                query = query.Where(x =>
                    (x.Email != null && x.Email.Contains(term)) ||
                    (x.UserName != null && x.UserName.Contains(term)) ||
                    (x.PhoneNumber != null && x.PhoneNumber.Contains(term)));
            }

            if (!string.IsNullOrWhiteSpace(roleFilter) && roleFilter != "all")
            {
                query = query.Where(x => x.UserRoles.Any(r => r.Role != null && r.Role.Name == roleFilter));
            }

            if (branchFilter.HasValue && branchFilter.Value > 0)
            {
                query = query.Where(x => x.BranchId == branchFilter.Value);
            }

            var now = DateTimeOffset.UtcNow;

            return await query
                .OrderBy(x => x.Email)
                .Select(x => new UserListItemViewModel
                {
                    Id = x.Id,
                    Email = x.Email ?? x.UserName ?? "-",
                    PhoneNumber = x.PhoneNumber,
                    RoleName = x.UserRoles
                        .Select(r => r.Role != null ? r.Role.Name : "-")
                        .FirstOrDefault() ?? "-",
                    BranchName = x.Branch != null ? x.Branch.Name : "-",
                    LockoutEnd = x.LockoutEnd,
                    IsDisabled = x.LockoutEnd != null && x.LockoutEnd > now
                })
                .ToListAsync();
        }

        public async Task<UserFormModel?> GetUserFormAsync(int id)
        {
            var tenantId = await _tenantContext.GetTenantIdAsync();

            if (tenantId == null)
            {
                return null;
            }

            var user = await _db.Users
                .AsNoTracking()
                .Include(x => x.UserRoles)
                    .ThenInclude(x => x.Role)
                .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId.Value);

            if (user == null)
            {
                return null;
            }

            return new UserFormModel
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber,
                BranchId = user.BranchId,
                RoleName = user.UserRoles
                    .Select(x => x.Role != null ? x.Role.Name : "Sales User")
                    .FirstOrDefault() ?? "Sales User"
            };
        }

        public async Task<(bool Success, string Message)> SaveUserAsync(UserFormModel model)
        {
            var tenantId = await _tenantContext.GetTenantIdAsync();

            if (tenantId == null)
            {
                return (false, "Tenant not found.");
            }

            await EnsureDefaultRolesAsync();
            await EnsureMainBranchExistsAsync(tenantId.Value);

            var email = model.Email.Trim();
            var normalizedEmail = email.ToUpperInvariant();

            var emailExists = await _db.Users.AnyAsync(x =>
                x.TenantId == tenantId.Value &&
                x.NormalizedEmail == normalizedEmail &&
                x.Id != (model.Id ?? 0));

            if (emailExists)
            {
                return (false, "A user with this email already exists.");
            }

            var role = await _db.Roles.FirstOrDefaultAsync(x =>
                x.TenantId == tenantId.Value &&
                x.Name == model.RoleName);

            if (role == null)
            {
                return (false, "Selected role was not found.");
            }

            var branchId = model.BranchId ?? await GetDefaultBranchIdAsync(tenantId.Value);

            if (!branchId.HasValue)
            {
                return (false, "No active branch found. Please create a branch first.");
            }

            var branch = await _db.Branches
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.Id == branchId.Value &&
                    x.TenantId == tenantId.Value &&
                    x.IsActive);

            if (branch == null)
            {
                return (false, "Selected branch was not found or is inactive.");
            }

            if (model.Id.HasValue && model.Id.Value > 0)
            {
                return await UpdateUserAsync(model, tenantId.Value, role.Id, role.Name, branch.Id, branch.Name);
            }

            if (string.IsNullOrWhiteSpace(model.Password))
            {
                return (false, "Password is required when creating a new user.");
            }

            var user = new ApplicationUser
            {
                TenantId = tenantId.Value,
                BranchId = branch.Id,
                UserName = email,
                NormalizedUserName = normalizedEmail,
                Email = email,
                NormalizedEmail = normalizedEmail,
                PhoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber) ? null : model.PhoneNumber.Trim(),
                EmailConfirmed = true,
                PhoneNumberConfirmed = false,
                LockoutEnabled = true,
                SecurityStamp = Guid.NewGuid().ToString(),
                ConcurrencyStamp = Guid.NewGuid().ToString()
            };

            user.PasswordHash = _passwordHasher.HashPassword(user, model.Password);

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            _db.UserRoles.Add(new UserRole
            {
                UserId = user.Id,
                RoleId = role.Id
            });

            await SyncUserClaimsAsync(user.Id, tenantId.Value, branch.Id);

            await _db.SaveChangesAsync();

            await _auditLogService.LogAsync(
                module: "Users",
                action: "Create",
                entityName: "ApplicationUser",
                entityId: user.Id,
                summary: $"Created user {email} with role {role.Name} at branch {branch.Name}.",
                oldValues: null,
                newValues: new
                {
                    user.Id,
                    user.Email,
                    user.PhoneNumber,
                    user.BranchId,
                    BranchName = branch.Name,
                    RoleName = role.Name,
                    user.LockoutEnabled,
                    user.LockoutEnd,
                    PasswordSet = true
                });

            return (true, "User created successfully.");
        }

        private async Task<(bool Success, string Message)> UpdateUserAsync(
            UserFormModel model,
            int tenantId,
            int roleId,
            string roleName,
            int branchId,
            string branchName)
        {
            var user = await _db.Users
                .Include(x => x.Branch)
                .Include(x => x.UserRoles)
                    .ThenInclude(x => x.Role)
                .FirstOrDefaultAsync(x => x.Id == model.Id!.Value && x.TenantId == tenantId);

            if (user == null)
            {
                return (false, "User not found.");
            }

            var oldRoleName = user.UserRoles
                .Select(x => x.Role != null ? x.Role.Name : "-")
                .FirstOrDefault() ?? "-";

            var oldValues = new
            {
                user.Email,
                user.PhoneNumber,
                user.BranchId,
                BranchName = user.Branch?.Name ?? "-",
                RoleName = oldRoleName,
                PasswordChanged = false
            };

            var email = model.Email.Trim();
            var normalizedEmail = email.ToUpperInvariant();

            var passwordChanged = !string.IsNullOrWhiteSpace(model.Password);

            user.Email = email;
            user.NormalizedEmail = normalizedEmail;
            user.UserName = email;
            user.NormalizedUserName = normalizedEmail;
            user.PhoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber) ? null : model.PhoneNumber.Trim();
            user.BranchId = branchId;
            user.ConcurrencyStamp = Guid.NewGuid().ToString();

            if (passwordChanged)
            {
                user.PasswordHash = _passwordHasher.HashPassword(user, model.Password!);
                user.SecurityStamp = Guid.NewGuid().ToString();
            }

            _db.UserRoles.RemoveRange(user.UserRoles);

            _db.UserRoles.Add(new UserRole
            {
                UserId = user.Id,
                RoleId = roleId
            });

            await SyncUserClaimsAsync(user.Id, tenantId, branchId);

            await _db.SaveChangesAsync();

            await _auditLogService.LogAsync(
                module: "Users",
                action: "Update",
                entityName: "ApplicationUser",
                entityId: user.Id,
                summary: $"Updated user {email}. Role: {oldRoleName} → {roleName}. Branch: {oldValues.BranchName} → {branchName}.",
                oldValues: oldValues,
                newValues: new
                {
                    user.Email,
                    user.PhoneNumber,
                    user.BranchId,
                    BranchName = branchName,
                    RoleName = roleName,
                    PasswordChanged = passwordChanged
                });

            return (true, "User updated successfully.");
        }

        public async Task<(bool Success, string Message)> ToggleUserStatusAsync(int userId)
        {
            var tenantId = await _tenantContext.GetTenantIdAsync();

            if (tenantId == null)
            {
                return (false, "Tenant not found.");
            }

            var user = await _db.Users
                .Include(x => x.Branch)
                .Include(x => x.UserRoles)
                    .ThenInclude(x => x.Role)
                .FirstOrDefaultAsync(x =>
                    x.Id == userId &&
                    x.TenantId == tenantId.Value);

            if (user == null)
            {
                return (false, "User not found.");
            }

            var now = DateTimeOffset.UtcNow;
            var wasDisabled = user.LockoutEnd != null && user.LockoutEnd > now;

            var oldValues = new
            {
                user.Email,
                user.LockoutEnabled,
                user.LockoutEnd,
                IsDisabled = wasDisabled
            };

            if (wasDisabled)
            {
                user.LockoutEnd = null;
                user.AccessFailedCount = 0;
            }
            else
            {
                user.LockoutEnd = now.AddYears(50);
                user.LockoutEnabled = true;
            }

            user.ConcurrencyStamp = Guid.NewGuid().ToString();

            await _db.SaveChangesAsync();

            var isNowDisabled = user.LockoutEnd != null && user.LockoutEnd > DateTimeOffset.UtcNow;
            var action = isNowDisabled ? "Deactivate" : "Activate";

            await _auditLogService.LogAsync(
                module: "Users",
                action: action,
                entityName: "ApplicationUser",
                entityId: user.Id,
                summary: $"{action}d user {user.Email ?? user.UserName ?? user.Id.ToString()}.",
                oldValues: oldValues,
                newValues: new
                {
                    user.Email,
                    user.LockoutEnabled,
                    user.LockoutEnd,
                    IsDisabled = isNowDisabled,
                    BranchName = user.Branch?.Name ?? "-",
                    RoleName = user.UserRoles
                        .Select(x => x.Role != null ? x.Role.Name : "-")
                        .FirstOrDefault() ?? "-"
                });

            return (true, wasDisabled ? "User activated successfully." : "User disabled successfully.");
        }

        private async Task EnsureMainBranchExistsAsync(int tenantId)
        {
            var hasBranch = await _db.Branches.AnyAsync(x => x.TenantId == tenantId);

            if (hasBranch)
            {
                return;
            }

            _db.Branches.Add(new Branch
            {
                TenantId = tenantId,
                Name = "Main Branch",
                BranchCode = "MAIN",
                IsMainBranch = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();
        }

        private async Task<int?> GetDefaultBranchIdAsync(int tenantId)
        {
            var branchId = await _db.Branches
                .Where(x => x.TenantId == tenantId && x.IsActive)
                .OrderByDescending(x => x.IsMainBranch)
                .ThenBy(x => x.Name)
                .Select(x => (int?)x.Id)
                .FirstOrDefaultAsync();

            return branchId;
        }

        private async Task SyncUserClaimsAsync(int userId, int tenantId, int branchId)
        {
            await UpsertClaimAsync(userId, "tenant_id", tenantId.ToString());
            await UpsertClaimAsync(userId, "branch_id", branchId.ToString());
        }

        private async Task UpsertClaimAsync(int userId, string claimType, string claimValue)
        {
            var claim = await _db.UserClaims.FirstOrDefaultAsync(x =>
                x.UserId == userId &&
                x.ClaimType == claimType);

            if (claim == null)
            {
                _db.UserClaims.Add(new IdentityUserClaim<int>
                {
                    UserId = userId,
                    ClaimType = claimType,
                    ClaimValue = claimValue
                });

                return;
            }

            claim.ClaimValue = claimValue;
        }

        private static string NormalizeRoleName(int tenantId, string roleName)
        {
            return $"{tenantId}:{roleName}".ToUpperInvariant();
        }
    }
}