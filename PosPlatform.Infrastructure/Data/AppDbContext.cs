using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PosPlatform.Domain.Entities;

namespace PosPlatform.Infrastructure.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser, Role, int,
        IdentityUserClaim<int>,
        UserRole,
        IdentityUserLogin<int>,
        IdentityRoleClaim<int>,
        IdentityUserToken<int>>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Tenant> Tenants => Set<Tenant>();
        public DbSet<SubscriptionPlan> SubscriptionPlans => Set<SubscriptionPlan>();
        public DbSet<Feature> Features => Set<Feature>();
        public DbSet<PlanFeature> PlanFeatures => Set<PlanFeature>();
        public DbSet<TenantSubscription> TenantSubscriptions => Set<TenantSubscription>();
        public DbSet<Branch> Branches => Set<Branch>();
        public DbSet<Permission> Permissions => Set<Permission>();
        public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

        public DbSet<Product> Products => Set<Product>();
        public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();

        public DbSet<Sale> Sales => Set<Sale>();
        public DbSet<SaleItem> SaleItems => Set<SaleItem>();
        public DbSet<StockMovement> StockMovements => Set<StockMovement>();
        public DbSet<BusinessSettings> BusinessSettings => Set<BusinessSettings>();

        public DbSet<CashierShift> CashierShifts => Set<CashierShift>();


        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<ApplicationUser>(entity =>
            {
                entity.ToTable("Users");

                entity.HasOne(x => x.Tenant)
                      .WithMany(x => x.Users)
                      .HasForeignKey(x => x.TenantId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.Branch)
                      .WithMany(x => x.Users)
                      .HasForeignKey(x => x.BranchId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<Role>(entity =>
            {
                entity.ToTable("Roles");

                entity.HasOne(x => x.Tenant)
                      .WithMany(x => x.Roles)
                      .HasForeignKey(x => x.TenantId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<UserRole>(entity =>
            {
                entity.ToTable("UserRoles");

                entity.HasOne(x => x.User)
                      .WithMany(x => x.UserRoles)
                      .HasForeignKey(x => x.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(x => x.Role)
                      .WithMany(x => x.UserRoles)
                      .HasForeignKey(x => x.RoleId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<IdentityUserClaim<int>>().ToTable("UserClaims");
            builder.Entity<IdentityUserLogin<int>>().ToTable("UserLogins");
            builder.Entity<IdentityUserToken<int>>().ToTable("UserTokens");
            builder.Entity<IdentityRoleClaim<int>>().ToTable("RoleClaims");

            builder.Entity<Tenant>(entity =>
            {
                entity.ToTable("Tenants");
                entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
                entity.Property(x => x.BusinessType).HasMaxLength(100);
                entity.Property(x => x.Email).HasMaxLength(150);
                entity.Property(x => x.Phone).HasMaxLength(50);
            });

            builder.Entity<SubscriptionPlan>(entity =>
            {
                entity.ToTable("SubscriptionPlans");
                entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
                entity.Property(x => x.PriceMonthly).HasColumnType("decimal(18,2)");
                entity.Property(x => x.PriceYearly).HasColumnType("decimal(18,2)");
            });

            builder.Entity<Feature>(entity =>
            {
                entity.ToTable("Features");
                entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
                entity.Property(x => x.Code).HasMaxLength(100).IsRequired();
                entity.HasIndex(x => x.Code).IsUnique();
            });

            builder.Entity<PlanFeature>(entity =>
            {
                entity.ToTable("PlanFeatures");

                entity.HasOne(x => x.SubscriptionPlan)
                      .WithMany(x => x.PlanFeatures)
                      .HasForeignKey(x => x.SubscriptionPlanId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(x => x.Feature)
                      .WithMany(x => x.PlanFeatures)
                      .HasForeignKey(x => x.FeatureId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(x => new { x.SubscriptionPlanId, x.FeatureId }).IsUnique();
            });

            builder.Entity<TenantSubscription>(entity =>
            {
                entity.ToTable("TenantSubscriptions");

                entity.HasOne(x => x.Tenant)
                      .WithMany(x => x.TenantSubscriptions)
                      .HasForeignKey(x => x.TenantId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(x => x.SubscriptionPlan)
                      .WithMany(x => x.TenantSubscriptions)
                      .HasForeignKey(x => x.SubscriptionPlanId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<Branch>(entity =>
            {
                entity.ToTable("Branches");
                entity.Property(x => x.Name).HasMaxLength(150).IsRequired();
                entity.Property(x => x.Code).HasMaxLength(50).IsRequired();

                entity.HasOne(x => x.Tenant)
                      .WithMany(x => x.Branches)
                      .HasForeignKey(x => x.TenantId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
            });

            builder.Entity<Permission>(entity =>
            {
                entity.ToTable("Permissions");
                entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
                entity.Property(x => x.Code).HasMaxLength(120).IsRequired();
                entity.HasIndex(x => x.Code).IsUnique();
            });

            builder.Entity<RolePermission>(entity =>
            {
                entity.ToTable("RolePermissions");

                entity.HasOne(x => x.Role)
                      .WithMany(x => x.RolePermissions)
                      .HasForeignKey(x => x.RoleId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(x => x.Permission)
                      .WithMany(x => x.RolePermissions)
                      .HasForeignKey(x => x.PermissionId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(x => new { x.RoleId, x.PermissionId }).IsUnique();
            });

            builder.Entity<ProductCategory>(entity =>
            {
                entity.ToTable("ProductCategories");

                entity.Property(x => x.Name)
                      .HasMaxLength(120)
                      .IsRequired();

                entity.Property(x => x.Description)
                      .HasMaxLength(300);

                entity.HasIndex(x => new { x.TenantId, x.Name }).IsUnique();

                entity.HasOne<Tenant>()
                      .WithMany()
                      .HasForeignKey(x => x.TenantId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<Product>(entity =>
            {
                entity.ToTable("Products");

                entity.Property(x => x.ProductName)
                      .HasMaxLength(150)
                      .IsRequired();

                entity.Property(x => x.SKU)
                      .HasMaxLength(80)
                      .IsRequired();

                entity.Property(x => x.Barcode)
                      .HasMaxLength(80);

                entity.Property(x => x.Description)
                      .HasMaxLength(500);

                entity.Property(x => x.ProductType)
      .HasMaxLength(50)
      .IsRequired();

                entity.Property(x => x.UnitOfMeasure)
                      .HasMaxLength(50);

                entity.Property(x => x.CostPrice)
                      .HasColumnType("decimal(18,2)");

                entity.Property(x => x.SellingPrice)
                      .HasColumnType("decimal(18,2)");

                entity.Property(x => x.QuantityInStock)
                      .HasColumnType("decimal(18,2)");

                entity.Property(x => x.ReorderLevel)
                      .HasColumnType("decimal(18,2)");

                entity.HasIndex(x => new { x.TenantId, x.SKU }).IsUnique();

                entity.HasOne(x => x.ProductCategory)
                      .WithMany(x => x.Products)
                      .HasForeignKey(x => x.ProductCategoryId)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(x => x.Branch)
                      .WithMany()
                      .HasForeignKey(x => x.BranchId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne<Tenant>()
      .WithMany()
      .HasForeignKey(x => x.TenantId)
      .OnDelete(DeleteBehavior.Restrict);
            });

           
            builder.Entity<SaleItem>(entity =>
            {
                entity.ToTable("SaleItems");

                entity.Property(x => x.ProductName)
                      .HasMaxLength(150)
                      .IsRequired();

                entity.Property(x => x.SKU)
                      .HasMaxLength(80)
                      .IsRequired();

                entity.Property(x => x.Quantity).HasColumnType("decimal(18,2)");
                entity.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)");
                entity.Property(x => x.LineTotal).HasColumnType("decimal(18,2)");

                entity.HasOne(x => x.Sale)
                      .WithMany(x => x.SaleItems)
                      .HasForeignKey(x => x.SaleId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(x => x.Product)
                      .WithMany()
                      .HasForeignKey(x => x.ProductId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<StockMovement>(entity =>
            {
                entity.ToTable("StockMovements");

                entity.Property(x => x.MovementType)
                      .HasMaxLength(50)
                      .IsRequired();

                entity.Property(x => x.ReferenceType)
                      .HasMaxLength(50)
                      .IsRequired();

                entity.Property(x => x.Notes)
                      .HasMaxLength(300);

                entity.Property(x => x.Quantity).HasColumnType("decimal(18,2)");
                entity.Property(x => x.QuantityBefore).HasColumnType("decimal(18,2)");
                entity.Property(x => x.QuantityAfter).HasColumnType("decimal(18,2)");

                entity.HasOne(x => x.Tenant)
                      .WithMany()
                      .HasForeignKey(x => x.TenantId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.Branch)
                      .WithMany()
                      .HasForeignKey(x => x.BranchId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.Product)
                      .WithMany()
                      .HasForeignKey(x => x.ProductId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
            builder.Entity<Sale>(entity =>
            {
                entity.ToTable("Sales");

                entity.Property(x => x.SaleNumber)
                      .HasMaxLength(60)
                      .IsRequired();

                entity.Property(x => x.CashierName)
                      .HasMaxLength(150);

                entity.Property(x => x.CustomerName)
                      .HasMaxLength(150);

                entity.Property(x => x.CustomerPhone)
                      .HasMaxLength(50);

                entity.Property(x => x.Notes)
                      .HasMaxLength(300);

                entity.Property(x => x.Subtotal).HasColumnType("decimal(18,2)");
                entity.Property(x => x.DiscountAmount).HasColumnType("decimal(18,2)");
                entity.Property(x => x.TaxAmount).HasColumnType("decimal(18,2)");
                entity.Property(x => x.TotalAmount).HasColumnType("decimal(18,2)");
                entity.Property(x => x.AmountPaid).HasColumnType("decimal(18,2)");
                entity.Property(x => x.ChangeAmount).HasColumnType("decimal(18,2)");

                entity.Property(x => x.PaymentMethod)
                      .HasMaxLength(50)
                      .IsRequired();

                entity.Property(x => x.Status)
                      .HasMaxLength(40)
                      .IsRequired();

                entity.HasIndex(x => new { x.TenantId, x.SaleNumber }).IsUnique();

                entity.HasOne(x => x.Tenant)
                      .WithMany()
                      .HasForeignKey(x => x.TenantId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.Branch)
                      .WithMany()
                      .HasForeignKey(x => x.BranchId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
            builder.Entity<BusinessSettings>(entity =>
            {
                entity.ToTable("BusinessSettings");

                entity.Property(x => x.BusinessName)
                      .HasMaxLength(200)
                      .IsRequired();

                entity.Property(x => x.BusinessType)
                      .HasMaxLength(100)
                      .IsRequired();

                entity.Property(x => x.Address)
                      .HasMaxLength(300);

                entity.Property(x => x.Phone)
                      .HasMaxLength(50);

                entity.Property(x => x.Email)
                      .HasMaxLength(150);

                entity.Property(x => x.CurrencyCode)
                      .HasMaxLength(10)
                      .IsRequired();

                entity.Property(x => x.CurrencySymbol)
                      .HasMaxLength(10)
                      .IsRequired();

                entity.Property(x => x.TaxName)
                      .HasMaxLength(50)
                      .IsRequired();

                entity.Property(x => x.TaxRate)
                      .HasColumnType("decimal(18,2)");

                entity.Property(x => x.ReceiptTitle)
                      .HasMaxLength(100)
                      .IsRequired();

                entity.Property(x => x.ReceiptFooterMessage)
                      .HasMaxLength(300);

                entity.Property(x => x.ReturnPolicyText)
                      .HasMaxLength(500);

                entity.HasIndex(x => x.TenantId)
                      .IsUnique();

                entity.HasOne(x => x.Tenant)
                      .WithMany()
                      .HasForeignKey(x => x.TenantId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<CashierShift>(entity =>
            {
                entity.ToTable("CashierShifts");

                entity.Property(x => x.CashierName)
                      .HasMaxLength(150)
                      .IsRequired();

                entity.Property(x => x.OpeningCash).HasColumnType("decimal(18,2)");
                entity.Property(x => x.ClosingCash).HasColumnType("decimal(18,2)");
                entity.Property(x => x.CashSales).HasColumnType("decimal(18,2)");
                entity.Property(x => x.CardSales).HasColumnType("decimal(18,2)");
                entity.Property(x => x.EftSales).HasColumnType("decimal(18,2)");
                entity.Property(x => x.TotalSales).HasColumnType("decimal(18,2)");
                entity.Property(x => x.ExpectedCash).HasColumnType("decimal(18,2)");
                entity.Property(x => x.CashDifference).HasColumnType("decimal(18,2)");

                entity.Property(x => x.Status)
                      .HasMaxLength(30)
                      .IsRequired();

                entity.Property(x => x.OpeningNotes)
                      .HasMaxLength(300);

                entity.Property(x => x.ClosingNotes)
                      .HasMaxLength(300);

                entity.HasIndex(x => new { x.TenantId, x.CashierUserId, x.Status });

                entity.HasOne(x => x.Tenant)
                      .WithMany()
                      .HasForeignKey(x => x.TenantId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.Branch)
                      .WithMany()
                      .HasForeignKey(x => x.BranchId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.CashierUser)
                      .WithMany()
                      .HasForeignKey(x => x.CashierUserId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}