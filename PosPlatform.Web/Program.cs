using Microsoft.AspNetCore.Components.Authorization;
using PosPlatform.Application.Interfaces;
using PosPlatform.Infrastructure;
using PosPlatform.Web.Components;
using PosPlatform.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PosPlatform.Domain.Entities;
using PosPlatform.Web.Models.Email;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddAuthorization();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<AuthenticationStateProvider, ServerAuthenticationStateProvider>();
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddControllersWithViews();

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddScoped<TenantContextService>();
builder.Services.AddScoped<SaleService>();
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<StockService>();

builder.Services.TryAddScoped<IPasswordHasher<ApplicationUser>, PasswordHasher<ApplicationUser>>();
builder.Services.AddScoped<UserManagementService>();
builder.Services.AddScoped<UserAccessService>();
builder.Services.AddScoped<BusinessSettingsService>();
builder.Services.AddScoped<CashierShiftService>();
builder.Services.AddScoped<CustomerService>();
builder.Services.AddScoped<ReportsService>();
builder.Services.AddScoped<RefundService>();
builder.Services.AddScoped<SupplierService>();
builder.Services.AddScoped<StockPurchaseService>();
builder.Services.AddScoped<ExpenseService>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<BranchService>();
builder.Services.AddScoped<StockTransferService>();
builder.Services.AddScoped<AuditLogService>();
builder.Services.AddScoped<QuoteService>();
builder.Services.AddScoped<InvoiceService>();
builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection("EmailSettings"));

builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<DocumentEmailService>();
builder.Services.AddScoped<CustomerStatementService>();
builder.Services.AddScoped<OverdueInvoiceService>();
builder.Services.AddScoped<QuoteInvoiceDashboardService>();
builder.Services.AddScoped<InvoiceFollowUpService>();
builder.Services.AddScoped<ProductVariantService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<IAppDbSeeder>();
    await seeder.SeedAsync();
}

app.Run();