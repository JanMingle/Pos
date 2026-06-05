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

        public DbSet<Customer> Customers => Set<Customer>();

        public DbSet<SaleReturn> SaleReturns => Set<SaleReturn>();
        public DbSet<SaleReturnItem> SaleReturnItems => Set<SaleReturnItem>();

        public DbSet<Supplier> Suppliers => Set<Supplier>();
        public DbSet<StockPurchase> StockPurchases => Set<StockPurchase>();
        public DbSet<StockPurchaseItem> StockPurchaseItems => Set<StockPurchaseItem>();

        public DbSet<ExpenseCategory> ExpenseCategories => Set<ExpenseCategory>();
        public DbSet<Expense> Expenses => Set<Expense>();

        public DbSet<StockTransfer> StockTransfers => Set<StockTransfer>();
        public DbSet<StockTransferItem> StockTransferItems => Set<StockTransferItem>();



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

                entity.Property(x => x.Name)
                      .HasMaxLength(150)
                      .IsRequired();

                entity.Property(x => x.BranchCode)
                      .HasMaxLength(50);

                entity.Property(x => x.Phone)
                      .HasMaxLength(50);

                entity.Property(x => x.Email)
                      .HasMaxLength(150);

                entity.Property(x => x.Address)
                      .HasMaxLength(300);

                entity.Property(x => x.Notes)
                      .HasMaxLength(500);

                entity.HasIndex(x => new { x.TenantId, x.Name });

                entity.HasOne(x => x.Tenant)
                      .WithMany()
                      .HasForeignKey(x => x.TenantId)
                      .OnDelete(DeleteBehavior.Restrict);
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

                entity.HasIndex(x => new { x.TenantId, x.BranchId, x.SKU })
      .IsUnique();

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

                entity.Property(x => x.UnitCost)
      .HasColumnType("decimal(18,2)");

                entity.Property(x => x.CostTotal)
                      .HasColumnType("decimal(18,2)");
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

                entity.HasOne(x => x.Customer)
      .WithMany(x => x.Sales)
      .HasForeignKey(x => x.CustomerId)
      .OnDelete(DeleteBehavior.SetNull);
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

            builder.Entity<Customer>(entity =>
            {
                entity.ToTable("Customers");

                entity.Property(x => x.CustomerType)
                      .HasMaxLength(30)
                      .IsRequired();

                entity.Property(x => x.FirstName)
                      .HasMaxLength(100);

                entity.Property(x => x.LastName)
                      .HasMaxLength(100);

                entity.Property(x => x.BusinessName)
                      .HasMaxLength(180);

                entity.Property(x => x.Phone)
                      .HasMaxLength(50);

                entity.Property(x => x.Email)
                      .HasMaxLength(150);

                entity.Property(x => x.Notes)
                      .HasMaxLength(500);

                entity.HasIndex(x => new { x.TenantId, x.Phone });
                entity.HasIndex(x => new { x.TenantId, x.Email });

                entity.HasOne(x => x.Tenant)
                      .WithMany()
                      .HasForeignKey(x => x.TenantId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.Branch)
                      .WithMany()
                      .HasForeignKey(x => x.BranchId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
            builder.Entity<SaleReturn>(entity =>
            {
                entity.ToTable("SaleReturns");

                entity.Property(x => x.ReturnNumber)
                      .HasMaxLength(80)
                      .IsRequired();

                entity.Property(x => x.ReturnType)
                      .HasMaxLength(30)
                      .IsRequired();

                entity.Property(x => x.RefundMethod)
                      .HasMaxLength(50)
                      .IsRequired();

                entity.Property(x => x.Status)
                      .HasMaxLength(30)
                      .IsRequired();

                entity.Property(x => x.ReturnedByName)
                      .HasMaxLength(150)
                      .IsRequired();

                entity.Property(x => x.Reason)
                      .HasMaxLength(500);

                entity.Property(x => x.TotalRefundAmount)
                      .HasColumnType("decimal(18,2)");

                entity.HasIndex(x => new { x.TenantId, x.ReturnNumber }).IsUnique();

                entity.HasOne(x => x.Tenant)
                      .WithMany()
                      .HasForeignKey(x => x.TenantId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.Branch)
                      .WithMany()
                      .HasForeignKey(x => x.BranchId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.Sale)
                      .WithMany(x => x.SaleReturns)
                      .HasForeignKey(x => x.SaleId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.ReturnedByUser)
                      .WithMany()
                      .HasForeignKey(x => x.ReturnedByUserId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<SaleReturnItem>(entity =>
            {
                entity.ToTable("SaleReturnItems");

                entity.Property(x => x.ProductName)
                      .HasMaxLength(150)
                      .IsRequired();

                entity.Property(x => x.SKU)
                      .HasMaxLength(80)
                      .IsRequired();

                entity.Property(x => x.ProductType)
                      .HasMaxLength(50)
                      .IsRequired();

                entity.Property(x => x.UnitOfMeasure)
                      .HasMaxLength(50);

                entity.Property(x => x.Quantity)
                      .HasColumnType("decimal(18,2)");

                entity.Property(x => x.UnitPrice)
                      .HasColumnType("decimal(18,2)");

                entity.Property(x => x.LineTotal)
                      .HasColumnType("decimal(18,2)");

                entity.HasOne(x => x.SaleReturn)
                      .WithMany(x => x.SaleReturnItems)
                      .HasForeignKey(x => x.SaleReturnId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(x => x.SaleItem)
                      .WithMany()
                      .HasForeignKey(x => x.SaleItemId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.Product)
                      .WithMany()
                      .HasForeignKey(x => x.ProductId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.Property(x => x.UnitCost)
      .HasColumnType("decimal(18,2)");

                entity.Property(x => x.CostTotal)
                      .HasColumnType("decimal(18,2)");
            });
            builder.Entity<Supplier>(entity =>
            {
                entity.ToTable("Suppliers");

                entity.Property(x => x.SupplierName)
                      .HasMaxLength(180)
                      .IsRequired();

                entity.Property(x => x.ContactPerson)
                      .HasMaxLength(150);

                entity.Property(x => x.Phone)
                      .HasMaxLength(50);

                entity.Property(x => x.Email)
                      .HasMaxLength(150);

                entity.Property(x => x.Address)
                      .HasMaxLength(300);

                entity.Property(x => x.TaxNumber)
                      .HasMaxLength(80);

                entity.Property(x => x.Notes)
                      .HasMaxLength(500);

                entity.HasIndex(x => new { x.TenantId, x.SupplierName });

                entity.HasOne(x => x.Tenant)
                      .WithMany()
                      .HasForeignKey(x => x.TenantId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.Branch)
                      .WithMany()
                      .HasForeignKey(x => x.BranchId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<StockPurchase>(entity =>
            {
                entity.ToTable("StockPurchases");

                entity.Property(x => x.PurchaseNumber)
                      .HasMaxLength(80)
                      .IsRequired();

                entity.Property(x => x.SupplierInvoiceNumber)
                      .HasMaxLength(100);

                entity.Property(x => x.Status)
                      .HasMaxLength(40)
                      .IsRequired();

                entity.Property(x => x.Notes)
                      .HasMaxLength(500);

                entity.Property(x => x.CreatedByName)
                      .HasMaxLength(150)
                      .IsRequired();

                entity.Property(x => x.Subtotal)
                      .HasColumnType("decimal(18,2)");

                entity.Property(x => x.TaxAmount)
                      .HasColumnType("decimal(18,2)");

                entity.Property(x => x.TotalAmount)
                      .HasColumnType("decimal(18,2)");

                entity.HasIndex(x => new { x.TenantId, x.PurchaseNumber }).IsUnique();

                entity.HasOne(x => x.Tenant)
                      .WithMany()
                      .HasForeignKey(x => x.TenantId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.Branch)
                      .WithMany()
                      .HasForeignKey(x => x.BranchId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.Supplier)
                      .WithMany(x => x.StockPurchases)
                      .HasForeignKey(x => x.SupplierId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.CreatedByUser)
                      .WithMany()
                      .HasForeignKey(x => x.CreatedByUserId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<StockPurchaseItem>(entity =>
            {
                entity.ToTable("StockPurchaseItems");

                entity.Property(x => x.ProductName)
                      .HasMaxLength(150)
                      .IsRequired();

                entity.Property(x => x.SKU)
                      .HasMaxLength(80)
                      .IsRequired();

                entity.Property(x => x.UnitOfMeasure)
                      .HasMaxLength(50);

                entity.Property(x => x.Quantity)
                      .HasColumnType("decimal(18,2)");

                entity.Property(x => x.UnitCost)
                      .HasColumnType("decimal(18,2)");

                entity.Property(x => x.LineTotal)
                      .HasColumnType("decimal(18,2)");

                entity.Property(x => x.QuantityBefore)
                      .HasColumnType("decimal(18,2)");

                entity.Property(x => x.QuantityAfter)
                      .HasColumnType("decimal(18,2)");

                entity.HasOne(x => x.StockPurchase)
                      .WithMany(x => x.StockPurchaseItems)
                      .HasForeignKey(x => x.StockPurchaseId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(x => x.Product)
                      .WithMany()
                      .HasForeignKey(x => x.ProductId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<ExpenseCategory>(entity =>
            {
                entity.ToTable("ExpenseCategories");

                entity.Property(x => x.CategoryName)
                      .HasMaxLength(120)
                      .IsRequired();

                entity.Property(x => x.Description)
                      .HasMaxLength(300);

                entity.HasIndex(x => new { x.TenantId, x.CategoryName });

                entity.HasOne(x => x.Tenant)
                      .WithMany()
                      .HasForeignKey(x => x.TenantId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.Branch)
                      .WithMany()
                      .HasForeignKey(x => x.BranchId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<Expense>(entity =>
            {
                entity.ToTable("Expenses");

                entity.Property(x => x.ExpenseNumber)
                      .HasMaxLength(80)
                      .IsRequired();

                entity.Property(x => x.VendorName)
                      .HasMaxLength(150);

                entity.Property(x => x.ReferenceNumber)
                      .HasMaxLength(100);

                entity.Property(x => x.PaymentMethod)
                      .HasMaxLength(50)
                      .IsRequired();

                entity.Property(x => x.Status)
                      .HasMaxLength(40)
                      .IsRequired();

                entity.Property(x => x.Notes)
                      .HasMaxLength(500);

                entity.Property(x => x.CreatedByName)
                      .HasMaxLength(150)
                      .IsRequired();

                entity.Property(x => x.Subtotal)
                      .HasColumnType("decimal(18,2)");

                entity.Property(x => x.TaxAmount)
                      .HasColumnType("decimal(18,2)");

                entity.Property(x => x.TotalAmount)
                      .HasColumnType("decimal(18,2)");

                entity.HasIndex(x => new { x.TenantId, x.ExpenseNumber }).IsUnique();

                entity.HasOne(x => x.Tenant)
                      .WithMany()
                      .HasForeignKey(x => x.TenantId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.Branch)
                      .WithMany()
                      .HasForeignKey(x => x.BranchId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.ExpenseCategory)
                      .WithMany(x => x.Expenses)
                      .HasForeignKey(x => x.ExpenseCategoryId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.CreatedByUser)
                      .WithMany()
                      .HasForeignKey(x => x.CreatedByUserId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<StockTransfer>(entity =>
            {
                entity.ToTable("StockTransfers");

                entity.Property(x => x.TransferNumber)
                    .HasMaxLength(80)
                    .IsRequired();

                entity.Property(x => x.Status)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(x => x.Notes)
                    .HasMaxLength(500);

                entity.Property(x => x.CreatedByName)
                    .HasMaxLength(150);

                entity.HasIndex(x => new { x.TenantId, x.TransferNumber });

                entity.HasOne(x => x.Tenant)
                    .WithMany()
                    .HasForeignKey(x => x.TenantId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.SourceBranch)
                    .WithMany()
                    .HasForeignKey(x => x.SourceBranchId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.DestinationBranch)
                    .WithMany()
                    .HasForeignKey(x => x.DestinationBranchId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<StockTransferItem>(entity =>
            {
                entity.ToTable("StockTransferItems");

                entity.Property(x => x.ProductName)
                    .HasMaxLength(150)
                    .IsRequired();

                entity.Property(x => x.SKU)
                    .HasMaxLength(80)
                    .IsRequired();

                entity.Property(x => x.Quantity)
                    .HasColumnType("decimal(18,2)");

                entity.Property(x => x.SourceQuantityBefore)
                    .HasColumnType("decimal(18,2)");

                entity.Property(x => x.SourceQuantityAfter)
                    .HasColumnType("decimal(18,2)");

                entity.Property(x => x.DestinationQuantityBefore)
                    .HasColumnType("decimal(18,2)");

                entity.Property(x => x.DestinationQuantityAfter)
                    .HasColumnType("decimal(18,2)");

                entity.Property(x => x.UnitOfMeasure)
                    .HasMaxLength(50);

                entity.HasOne(x => x.StockTransfer)
                    .WithMany(x => x.Items)
                    .HasForeignKey(x => x.StockTransferId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(x => x.SourceProduct)
                    .WithMany()
                    .HasForeignKey(x => x.SourceProductId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.TargetProduct)
                    .WithMany()
                    .HasForeignKey(x => x.TargetProductId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

        }
    }
}