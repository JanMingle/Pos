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
            IPasswordHasher<ApplicationUser> passwordHasher)
        {
            _db = db;
            _tenantContext = tenantContext;
            _passwordHasher = passwordHasher;
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

            return await _db.Branches
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId.Value)
                .OrderBy(x => x.Name)
                .Select(x => new BranchOptionViewModel
                {
                    Id = x.Id,
                    Name = x.Name
                })
                .ToListAsync();
        }

        public async Task<List<UserListItemViewModel>> GetUsersAsync(string? search, string? roleFilter)
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
            var defaultBranchId = await _tenantContext.GetBranchIdAsync();

            if (tenantId == null)
            {
                return (false, "Tenant not found.");
            }

            await EnsureDefaultRolesAsync();

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

            var branchId = model.BranchId ?? defaultBranchId;

            if (branchId.HasValue)
            {
                var branchExists = await _db.Branches.AnyAsync(x =>
                    x.Id == branchId.Value &&
                    x.TenantId == tenantId.Value);

                if (!branchExists)
                {
                    return (false, "Selected branch was not found.");
                }
            }

            if (model.Id.HasValue && model.Id.Value > 0)
            {
                return await UpdateUserAsync(model, tenantId.Value, role.Id, branchId);
            }

            if (string.IsNullOrWhiteSpace(model.Password))
            {
                return (false, "Password is required when creating a new user.");
            }

            var user = new ApplicationUser
            {
                TenantId = tenantId.Value,
                BranchId = branchId,
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

            await _db.SaveChangesAsync();

            return (true, "User created successfully.");
        }

        private async Task<(bool Success, string Message)> UpdateUserAsync(
            UserFormModel model,
            int tenantId,
            int roleId,
            int? branchId)
        {
            var user = await _db.Users
                .Include(x => x.UserRoles)
                .FirstOrDefaultAsync(x => x.Id == model.Id!.Value && x.TenantId == tenantId);

            if (user == null)
            {
                return (false, "User not found.");
            }

            var email = model.Email.Trim();
            var normalizedEmail = email.ToUpperInvariant();

            user.Email = email;
            user.NormalizedEmail = normalizedEmail;
            user.UserName = email;
            user.NormalizedUserName = normalizedEmail;
            user.PhoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber) ? null : model.PhoneNumber.Trim();
            user.BranchId = branchId;
            user.ConcurrencyStamp = Guid.NewGuid().ToString();

            if (!string.IsNullOrWhiteSpace(model.Password))
            {
                user.PasswordHash = _passwordHasher.HashPassword(user, model.Password);
                user.SecurityStamp = Guid.NewGuid().ToString();
            }

            _db.UserRoles.RemoveRange(user.UserRoles);

            _db.UserRoles.Add(new UserRole
            {
                UserId = user.Id,
                RoleId = roleId
            });

            await _db.SaveChangesAsync();

            return (true, "User updated successfully.");
        }

        public async Task<(bool Success, string Message)> ToggleUserStatusAsync(int userId)
        {
            var tenantId = await _tenantContext.GetTenantIdAsync();

            if (tenantId == null)
            {
                return (false, "Tenant not found.");
            }

            var user = await _db.Users.FirstOrDefaultAsync(x =>
                x.Id == userId &&
                x.TenantId == tenantId.Value);

            if (user == null)
            {
                return (false, "User not found.");
            }

            var now = DateTimeOffset.UtcNow;
            var isDisabled = user.LockoutEnd != null && user.LockoutEnd > now;

            if (isDisabled)
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

            return (true, isDisabled ? "User activated successfully." : "User disabled successfully.");
        }

        private static string NormalizeRoleName(int tenantId, string roleName)
        {
            return $"{tenantId}:{roleName}".ToUpperInvariant();
        }
    }
}