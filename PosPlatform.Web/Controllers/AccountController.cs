using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PosPlatform.Domain.Entities;
using PosPlatform.Infrastructure.Data;
using PosPlatform.Shared.Constants;
using PosPlatform.Web.Models.Auth;

namespace PosPlatform.Web.Controllers
{
    [Route("account")]
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<Role> _roleManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly AppDbContext _db;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            RoleManager<Role> roleManager,
            SignInManager<ApplicationUser> signInManager,
            AppDbContext db)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _signInManager = signInManager;
            _db = db;
        }

        [HttpPost("login")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login([FromForm] LoginRequest model)
        {
            if (string.IsNullOrWhiteSpace(model.Email) || string.IsNullOrWhiteSpace(model.Password))
            {
                return Redirect("/login?error=Please enter email and password");
            }

            var email = model.Email.Trim();

            var user = await _userManager.FindByEmailAsync(email);

            if (user == null || !user.IsActive)
            {
                return Redirect("/login?error=Invalid login details");
            }

            var result = await _signInManager.PasswordSignInAsync(
                user.UserName!,
                model.Password,
                isPersistent: true,
                lockoutOnFailure: false);

            if (!result.Succeeded)
            {
                return Redirect("/login?error=Invalid login details");
            }

            if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }

            return Redirect("/dashboard");
        }

        [HttpPost("logout")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return Redirect("/login");
        }

        [HttpPost("register-business")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterBusiness([FromForm] RegisterBusinessRequest model)
        {
            if (string.IsNullOrWhiteSpace(model.BusinessName) ||
                string.IsNullOrWhiteSpace(model.BusinessType) ||
                string.IsNullOrWhiteSpace(model.OwnerFullName) ||
                string.IsNullOrWhiteSpace(model.OwnerEmail) ||
                string.IsNullOrWhiteSpace(model.Password))
            {
                return Redirect("/register-business?error=Please complete all required fields");
            }

            if (model.TaxEnabled && model.TaxRate <= 0)
            {
                return Redirect("/register-business?error=Tax rate must be greater than zero when tax is enabled");
            }

            var ownerEmail = model.OwnerEmail.Trim();

            var existingUser = await _userManager.FindByEmailAsync(ownerEmail);

            if (existingUser != null)
            {
                return Redirect("/register-business?error=Owner email already exists");
            }

            var selectedPlanId = model.SubscriptionPlanId
                                 ?? await _db.SubscriptionPlans
                                     .Where(x => x.Name == "Lite POS" && x.IsActive)
                                     .Select(x => (int?)x.Id)
                                     .FirstOrDefaultAsync();

            if (selectedPlanId == null)
            {
                return Redirect("/register-business?error=No active subscription plan found");
            }

            await using var tx = await _db.Database.BeginTransactionAsync();

            try
            {
                var businessName = model.BusinessName.Trim();
                var businessType = model.BusinessType.Trim();

                var tenant = new Tenant
                {
                    Name = businessName,
                    BusinessType = businessType,
                    Email = Clean(model.BusinessEmail),
                    Phone = Clean(model.BusinessPhone),
                    Address = Clean(model.BusinessAddress),
                    IsActive = true
                };

                _db.Tenants.Add(tenant);
                await _db.SaveChangesAsync();

                var branch = new Branch
                {
                    TenantId = tenant.Id,
                    Name = string.IsNullOrWhiteSpace(model.BranchName)
                        ? "Main Branch"
                        : model.BranchName.Trim(),
                    Code = string.IsNullOrWhiteSpace(model.BranchCode)
                        ? "MAIN"
                        : model.BranchCode.Trim().ToUpperInvariant(),
                    Address = Clean(model.BusinessAddress),
                    Phone = Clean(model.BusinessPhone),
                    IsActive = true
                };

                _db.Branches.Add(branch);
                await _db.SaveChangesAsync();

                var businessSettings = new BusinessSettings
                {
                    TenantId = tenant.Id,

                    BusinessName = businessName,
                    BusinessType = businessType,
                    Address = Clean(model.BusinessAddress),
                    Phone = Clean(model.BusinessPhone),
                    Email = Clean(model.BusinessEmail),

                    CurrencyCode = string.IsNullOrWhiteSpace(model.CurrencyCode)
                        ? "ZAR"
                        : model.CurrencyCode.Trim().ToUpperInvariant(),

                    CurrencySymbol = string.IsNullOrWhiteSpace(model.CurrencySymbol)
                        ? "R"
                        : model.CurrencySymbol.Trim(),

                    TaxEnabled = model.TaxEnabled,
                    TaxName = string.IsNullOrWhiteSpace(model.TaxName)
                        ? "VAT"
                        : model.TaxName.Trim(),
                    TaxRate = model.TaxEnabled ? model.TaxRate : 0,

                    ProductsEnabled = model.ProductsEnabled,
                    StockTrackingEnabled = model.StockTrackingEnabled,
                    ServicesEnabled = model.ServicesEnabled,
                    AppointmentsEnabled = model.AppointmentsEnabled,
                    CustomersEnabled = model.CustomersEnabled,
                    AgeRestrictedProductsEnabled = model.AgeRestrictedProductsEnabled,

                    AllowNegativeStock = model.AllowNegativeStock,
                    RequireCustomerForSale = model.RequireCustomerForSale,
                    AllowDiscounts = model.AllowDiscounts,

                    ReceiptTitle = string.IsNullOrWhiteSpace(model.ReceiptTitle)
                        ? "Sales Receipt"
                        : model.ReceiptTitle.Trim(),

                    ReceiptFooterMessage = string.IsNullOrWhiteSpace(model.ReceiptFooterMessage)
                        ? "Thank you for your purchase."
                        : model.ReceiptFooterMessage.Trim(),

                    ReturnPolicyText = Clean(model.ReturnPolicyText),

                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _db.BusinessSettings.Add(businessSettings);
                await _db.SaveChangesAsync();

                var tenantSubscription = new TenantSubscription
                {
                    TenantId = tenant.Id,
                    SubscriptionPlanId = selectedPlanId.Value,
                    StartDate = DateTime.UtcNow,
                    Status = "Active",
                    IsTrial = true
                };

                _db.TenantSubscriptions.Add(tenantSubscription);
                await _db.SaveChangesAsync();

                var ownerRoleName = SystemRoles.Owner;

                var ownerRole = await _db.Roles
                    .FirstOrDefaultAsync(r =>
                        r.TenantId == tenant.Id &&
                        r.Name == ownerRoleName);

                if (ownerRole == null)
                {
                    ownerRole = new Role
                    {
                        Name = ownerRoleName,
                        NormalizedName = NormalizeTenantRoleName(tenant.Id, ownerRoleName),
                        TenantId = tenant.Id,
                        Description = "Tenant owner",
                        ConcurrencyStamp = Guid.NewGuid().ToString()
                    };

                    _db.Roles.Add(ownerRole);
                    await _db.SaveChangesAsync();
                }

                var user = new ApplicationUser
                {
                    FullName = model.OwnerFullName.Trim(),
                    Email = ownerEmail,
                    UserName = ownerEmail,
                    TenantId = tenant.Id,
                    BranchId = branch.Id,
                    IsActive = true,
                    EmailConfirmed = true
                };

                var createUserResult = await _userManager.CreateAsync(user, model.Password);

                if (!createUserResult.Succeeded)
                {
                    await tx.RollbackAsync();

                    var firstError = createUserResult.Errors.FirstOrDefault()?.Description
                                     ?? "Failed to create user";

                    return Redirect($"/register-business?error={Uri.EscapeDataString(firstError)}");
                }

                var alreadyAssignedOwnerRole = await _db.UserRoles.AnyAsync(x =>
                    x.UserId == user.Id &&
                    x.RoleId == ownerRole.Id);

                if (!alreadyAssignedOwnerRole)
                {
                    _db.UserRoles.Add(new UserRole
                    {
                        UserId = user.Id,
                        RoleId = ownerRole.Id
                    });

                    await _db.SaveChangesAsync();
                }

                await _userManager.AddClaimsAsync(user, new[]
                {
                    new Claim("tenant_id", tenant.Id.ToString()),
                    new Claim("branch_id", branch.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.FullName ?? ownerEmail),
                    new Claim(ClaimTypes.Email, user.Email!)
                });

                await SeedStarterCategoriesAsync(tenant.Id, businessType);

                await tx.CommitAsync();

                await _signInManager.SignInAsync(user, isPersistent: true);

                return Redirect("/dashboard");
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();

                return Redirect($"/register-business?error={Uri.EscapeDataString("Registration failed: " + ex.Message)}");
            }
        }

        private async Task SeedStarterCategoriesAsync(int tenantId, string businessType)
        {
            var existingCategories = await _db.ProductCategories.AnyAsync(x => x.TenantId == tenantId);

            if (existingCategories)
            {
                return;
            }

            var categories = GetStarterCategories(tenantId, businessType);

            _db.ProductCategories.AddRange(categories);
            await _db.SaveChangesAsync();
        }

        private static List<ProductCategory> GetStarterCategories(int tenantId, string businessType)
        {
            var normalized = businessType.Trim().ToLowerInvariant();

            if (normalized.Contains("salon") || normalized.Contains("beauty"))
            {
                return new List<ProductCategory>
                {
                    NewCategory(tenantId, "Hair Services", "Haircuts, styling, braids and treatments."),
                    NewCategory(tenantId, "Beauty Services", "Nails, makeup, lashes and beauty treatments."),
                    NewCategory(tenantId, "Hair Products", "Shampoo, conditioner, treatments and hair care products."),
                    NewCategory(tenantId, "Salon Retail", "Products sold to customers after appointments."),
                    NewCategory(tenantId, "Packages", "Service bundles and promotional packages.")
                };
            }

            if (normalized.Contains("liquor"))
            {
                return new List<ProductCategory>
                {
                    NewCategory(tenantId, "Beer", "Beer products and packs."),
                    NewCategory(tenantId, "Wine", "Wine products."),
                    NewCategory(tenantId, "Spirits", "Whisky, vodka, gin, brandy and other spirits."),
                    NewCategory(tenantId, "Ciders & Coolers", "Ready-to-drink alcoholic beverages."),
                    NewCategory(tenantId, "Soft Drinks & Mixers", "Mixers, soft drinks and non-alcoholic beverages."),
                    NewCategory(tenantId, "Snacks", "Snacks and convenience products.")
                };
            }

            if (normalized.Contains("clothing"))
            {
                return new List<ProductCategory>
                {
                    NewCategory(tenantId, "Men", "Men's clothing and accessories."),
                    NewCategory(tenantId, "Women", "Women's clothing and accessories."),
                    NewCategory(tenantId, "Kids", "Kids clothing."),
                    NewCategory(tenantId, "Shoes", "Footwear."),
                    NewCategory(tenantId, "Accessories", "Bags, belts, hats and accessories.")
                };
            }

            if (normalized.Contains("grocery") || normalized.Contains("market"))
            {
                return new List<ProductCategory>
                {
                    NewCategory(tenantId, "Groceries", "General grocery items."),
                    NewCategory(tenantId, "Beverages", "Drinks and refreshments."),
                    NewCategory(tenantId, "Food", "Food items."),
                    NewCategory(tenantId, "Household", "Household essentials."),
                    NewCategory(tenantId, "Personal Care", "Personal care products.")
                };
            }

            if (normalized.Contains("service"))
            {
                return new List<ProductCategory>
                {
                    NewCategory(tenantId, "Services", "Services sold through the system."),
                    NewCategory(tenantId, "Packages", "Service bundles and packages."),
                    NewCategory(tenantId, "Consultation", "Consultation and advisory services."),
                    NewCategory(tenantId, "Products", "Products sold alongside services.")
                };
            }

            if (normalized.Contains("restaurant") || normalized.Contains("cafe") || normalized.Contains("coffee"))
            {
                return new List<ProductCategory>
                {
                    NewCategory(tenantId, "Food", "Meals and food items."),
                    NewCategory(tenantId, "Hot Drinks", "Coffee, tea and hot beverages."),
                    NewCategory(tenantId, "Cold Drinks", "Cold beverages."),
                    NewCategory(tenantId, "Snacks", "Snacks and sides."),
                    NewCategory(tenantId, "Services", "Additional service charges or fees.")
                };
            }

            return new List<ProductCategory>
            {
                NewCategory(tenantId, "General", "General products."),
                NewCategory(tenantId, "Beverages", "Drinks and refreshments."),
                NewCategory(tenantId, "Food", "Food items."),
                NewCategory(tenantId, "Services", "Services sold through the system."),
                NewCategory(tenantId, "Other", "Other items.")
            };
        }

        private static ProductCategory NewCategory(int tenantId, string name, string description)
        {
            return new ProductCategory
            {
                TenantId = tenantId,
                Name = name,
                Description = description,
                IsActive = true
            };
        }

        private static string NormalizeTenantRoleName(int tenantId, string roleName)
        {
            return $"{tenantId}:{roleName}".ToUpperInvariant();
        }

        private static string? Clean(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}