using Microsoft.EntityFrameworkCore;
using PosPlatform.Domain.Entities;
using PosPlatform.Infrastructure.Data;
using PosPlatform.Shared.Constants;

namespace PosPlatform.Infrastructure.Seed
{
    public static class AppDbSeeder
    {
        public static async Task SeedAsync(AppDbContext context)
        {
            await context.Database.MigrateAsync();

            if (!await context.Features.AnyAsync())
            {
                var features = new List<Feature>
                {
                    new() { Name = "Sales", Code = SystemFeatures.Sales, Description = "Basic sales module" },
                    new() { Name = "Inventory", Code = SystemFeatures.Inventory, Description = "Inventory and stock control" },
                    new() { Name = "Accounting", Code = SystemFeatures.Accounting, Description = "Accounting and ledgers" },
                    new() { Name = "Multi Branch", Code = SystemFeatures.MultiBranch, Description = "Multiple branches/outlets" },
                    new() { Name = "Customer Accounts", Code = SystemFeatures.CustomerAccounts, Description = "Customer credit/accounts" },
                    new() { Name = "Purchase Orders", Code = SystemFeatures.PurchaseOrders, Description = "Purchasing workflows" },
                    new() { Name = "Internet Cafe", Code = SystemFeatures.InternetCafe, Description = "Timed computer usage" },
                    new() { Name = "Restaurant Tables", Code = SystemFeatures.RestaurantTables, Description = "Table and order workflow" }
                };

                context.Features.AddRange(features);
                await context.SaveChangesAsync();
            }

            if (!await context.SubscriptionPlans.AnyAsync())
            {
                var lite = new SubscriptionPlan
                {
                    Name = "Lite POS",
                    PriceMonthly = 299,
                    PriceYearly = 2990,
                    Description = "Sales only package",
                    IsActive = true
                };

                var business = new SubscriptionPlan
                {
                    Name = "Business POS",
                    PriceMonthly = 699,
                    PriceYearly = 6990,
                    Description = "Sales, stock, reports, branch support",
                    IsActive = true
                };

                var enterprise = new SubscriptionPlan
                {
                    Name = "Enterprise POS",
                    PriceMonthly = 1499,
                    PriceYearly = 14990,
                    Description = "Full package with accounting and advanced modules",
                    IsActive = true
                };

                context.SubscriptionPlans.AddRange(lite, business, enterprise);
                await context.SaveChangesAsync();

                var features = await context.Features.ToListAsync();

                var liteFeatures = features
                    .Where(x => x.Code == SystemFeatures.Sales)
                    .Select(x => new PlanFeature { SubscriptionPlanId = lite.Id, FeatureId = x.Id });

                var businessFeatures = features
                    .Where(x => x.Code == SystemFeatures.Sales ||
                                x.Code == SystemFeatures.Inventory ||
                                x.Code == SystemFeatures.MultiBranch ||
                                x.Code == SystemFeatures.CustomerAccounts ||
                                x.Code == SystemFeatures.PurchaseOrders)
                    .Select(x => new PlanFeature { SubscriptionPlanId = business.Id, FeatureId = x.Id });

                var enterpriseFeatures = features
                    .Select(x => new PlanFeature { SubscriptionPlanId = enterprise.Id, FeatureId = x.Id });

                context.PlanFeatures.AddRange(liteFeatures);
                context.PlanFeatures.AddRange(businessFeatures);
                context.PlanFeatures.AddRange(enterpriseFeatures);

                await context.SaveChangesAsync();
            }

            if (!await context.Permissions.AnyAsync())
            {
                var permissions = new List<Permission>
                {
                    new() { Name = "View Sales", Code = SystemPermissions.ViewSales, Description = "Can view sales" },
                    new() { Name = "Make Sale", Code = SystemPermissions.MakeSale, Description = "Can make sales" },
                    new() { Name = "Refund Sale", Code = SystemPermissions.RefundSale, Description = "Can refund sales" },
                    new() { Name = "Manage Stock", Code = SystemPermissions.ManageStock, Description = "Can manage stock" },
                    new() { Name = "View Accounting", Code = SystemPermissions.ViewAccounting, Description = "Can view accounting" },
                    new() { Name = "Manage Users", Code = SystemPermissions.ManageUsers, Description = "Can manage users" },
                    new() { Name = "Manage Branches", Code = SystemPermissions.ManageBranches, Description = "Can manage branches" },
                    new() { Name = "Manage Settings", Code = SystemPermissions.ManageSettings, Description = "Can manage settings" }
                };

                context.Permissions.AddRange(permissions);
                await context.SaveChangesAsync();
            }
        }
    }
}