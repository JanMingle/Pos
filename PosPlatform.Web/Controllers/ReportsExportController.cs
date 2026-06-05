using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PosPlatform.Infrastructure.Data;
using System.Security.Claims;
using PosPlatform.Domain.Entities;

namespace PosPlatform.Web.Controllers
{
    [Authorize]
    [Route("reports/export")]
    public class ReportsExportController : Controller
    {
        private readonly AppDbContext _db;

        public ReportsExportController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet("excel")]
        public async Task<IActionResult> ExportExcel(DateTime? fromDate, DateTime? toDate, int? branchId)
        {
            var tenantId = await GetTenantIdAsync();

            if (tenantId == null)
            {
                return Unauthorized("Tenant not found.");
            }

            var from = fromDate?.Date ?? DateTime.Today;
            var toExclusive = (toDate?.Date ?? DateTime.Today).AddDays(1);
            var toDisplay = toExclusive.AddDays(-1);

            var branchName = "All Branches";

            if (branchId.HasValue)
            {
                var branch = await _db.Branches
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x =>
                        x.Id == branchId.Value &&
                        x.TenantId == tenantId.Value &&
                        x.IsActive);

                if (branch == null)
                {
                    return BadRequest("Selected branch was not found.");
                }

                branchName = branch.Name;
            }

            var settings = await _db.BusinessSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.TenantId == tenantId.Value);

            var currency = string.IsNullOrWhiteSpace(settings?.CurrencySymbol)
                ? "R"
                : settings.CurrencySymbol;

            var businessName = string.IsNullOrWhiteSpace(settings?.BusinessName)
                ? "POS Platform"
                : settings.BusinessName;

            var sales = await _db.Sales
                .AsNoTracking()
                .Include(x => x.SaleItems)
                .Where(x =>
                    x.TenantId == tenantId.Value &&
                    x.Status != "Voided" &&
                    x.CreatedAt >= from &&
                    x.CreatedAt < toExclusive &&
                    (!branchId.HasValue || x.BranchId == branchId.Value))
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            var refunds = await _db.SaleReturns
                .AsNoTracking()
                .Include(x => x.Sale)
                .Include(x => x.SaleReturnItems)
                .Where(x =>
                    x.TenantId == tenantId.Value &&
                    x.Status == "Completed" &&
                    x.CreatedAt >= from &&
                    x.CreatedAt < toExclusive &&
                    (!branchId.HasValue || x.BranchId == branchId.Value))
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            var expenses = await _db.Expenses
                .AsNoTracking()
                .Include(x => x.ExpenseCategory)
                .Where(x =>
                    x.TenantId == tenantId.Value &&
                    x.Status == "Recorded" &&
                    x.ExpenseDate >= from &&
                    x.ExpenseDate < toExclusive &&
                    (!branchId.HasValue || x.BranchId == branchId.Value))
                .OrderByDescending(x => x.ExpenseDate)
                .ThenByDescending(x => x.Id)
                .ToListAsync();

            var purchases = await _db.StockPurchases
                .AsNoTracking()
                .Include(x => x.Supplier)
                .Include(x => x.StockPurchaseItems)
                .Where(x =>
                    x.TenantId == tenantId.Value &&
                    x.PurchaseDate >= from &&
                    x.PurchaseDate < toExclusive &&
                    (!branchId.HasValue || x.BranchId == branchId.Value))
                .OrderByDescending(x => x.PurchaseDate)
                .ThenByDescending(x => x.Id)
                .ToListAsync();

            var stock = await _db.Products
                .AsNoTracking()
                .Include(x => x.ProductCategory)
                .Where(x =>
                    x.TenantId == tenantId.Value &&
                    x.TrackStock &&
                    (!branchId.HasValue || x.BranchId == null || x.BranchId == branchId.Value))
                .OrderBy(x => x.ProductName)
                .ToListAsync();

            var grossSales = sales.Sum(x => x.TotalAmount);
            var totalRefunds = refunds.Sum(x => x.TotalRefundAmount);
            var netSales = grossSales - totalRefunds;

            var soldCost = sales.SelectMany(x => x.SaleItems).Sum(x => x.CostTotal);
            var refundedCost = refunds.SelectMany(x => x.SaleReturnItems).Sum(x => x.CostTotal);
            var costOfGoods = soldCost - refundedCost;

            var grossProfit = netSales - costOfGoods;
            var totalExpenses = expenses.Sum(x => x.TotalAmount);
            var netProfit = grossProfit - totalExpenses;

            using var workbook = new XLWorkbook();

            BuildSummarySheet(
                workbook,
                businessName,
                branchName,
                currency,
                from,
                toDisplay,
                grossSales,
                totalRefunds,
                netSales,
                costOfGoods,
                grossProfit,
                totalExpenses,
                netProfit,
                sales.Count,
                refunds.Count,
                expenses.Count,
                purchases.Count,
                stock.Count);

            BuildSalesSheet(workbook, sales, currency);
            BuildRefundsSheet(workbook, refunds, currency);
            BuildExpensesSheet(workbook, expenses, currency);
            BuildPurchasesSheet(workbook, purchases, currency);
            BuildStockSheet(workbook, stock, currency);

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            var fileName = $"reports-{from:yyyyMMdd}-{toDisplay:yyyyMMdd}.xlsx";

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

        private async Task<int?> GetTenantIdAsync()
        {
            var tenantClaim = User.FindFirst("tenant_id")?.Value;

            if (int.TryParse(tenantClaim, out var tenantIdFromClaim))
            {
                return tenantIdFromClaim;
            }

            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdValue, out var userId))
            {
                return null;
            }

            return await _db.Users
                .AsNoTracking()
                .Where(x => x.Id == userId)
                .Select(x => (int?)x.TenantId)
                .FirstOrDefaultAsync();
        }

        private static void BuildSummarySheet(
            XLWorkbook workbook,
            string businessName,
            string branchName,
            string currency,
            DateTime from,
            DateTime to,
            decimal grossSales,
            decimal refunds,
            decimal netSales,
            decimal costOfGoods,
            decimal grossProfit,
            decimal expenses,
            decimal netProfit,
            int salesCount,
            int refundCount,
            int expenseCount,
            int purchaseCount,
            int stockCount)
        {
            var ws = workbook.Worksheets.Add("Summary");

            ws.Cell("A1").Value = businessName;
            ws.Cell("A2").Value = "Reports Export";
            ws.Cell("A4").Value = "Branch";
            ws.Cell("B4").Value = branchName;
            ws.Cell("A5").Value = "Date Range";
            ws.Cell("B5").Value = $"{from:yyyy-MM-dd} to {to:yyyy-MM-dd}";
            ws.Cell("A6").Value = "Generated";
            ws.Cell("B6").Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm");

            ws.Cell("A8").Value = "Metric";
            ws.Cell("B8").Value = "Value";

            var rows = new (string Label, object Value)[]
            {
                ("Gross Sales", grossSales),
                ("Refunds", refunds),
                ("Net Sales", netSales),
                ("Cost of Goods", costOfGoods),
                ("Gross Profit", grossProfit),
                ("Expenses", expenses),
                ("Net Profit", netProfit),
                ("Sales Count", salesCount),
                ("Refund Count", refundCount),
                ("Expense Count", expenseCount),
                ("Purchase Count", purchaseCount),
                ("Stock Items", stockCount)
            };

            var row = 9;

            foreach (var item in rows)
            {
                ws.Cell(row, 1).Value = item.Label;

                if (item.Value is decimal money)
                {
                    ws.Cell(row, 2).Value = money;
                    ws.Cell(row, 2).Style.NumberFormat.Format = $"{currency} #,##0.00";
                }
                else if (item.Value is int number)
                {
                    ws.Cell(row, 2).Value = number;
                }

                row++;
            }

            StyleTitle(ws, "A1:B2");
            StyleHeader(ws, "A8:B8");
            StyleUsedRange(ws);
        }

        private static void BuildSalesSheet(XLWorkbook workbook, List<Sale> sales, string currency)
        {
            var ws = workbook.Worksheets.Add("Sales");

            var headers = new[]
            {
                "Sale No", "Date", "Customer", "Payment", "Items", "Subtotal",
                "Discount", "Tax", "Total", "Paid", "Change", "Status"
            };

            WriteHeaders(ws, headers);

            var row = 2;

            foreach (var sale in sales)
            {
                ws.Cell(row, 1).Value = sale.SaleNumber;
                ws.Cell(row, 2).Value = sale.CreatedAt;
                ws.Cell(row, 3).Value = string.IsNullOrWhiteSpace(sale.CustomerName) ? "Walk-in" : sale.CustomerName;
                ws.Cell(row, 4).Value = sale.PaymentMethod;
                ws.Cell(row, 5).Value = sale.SaleItems.Count;
                ws.Cell(row, 6).Value = sale.TotalAmount - sale.TaxAmount + sale.DiscountAmount;
                ws.Cell(row, 7).Value = sale.DiscountAmount;
                ws.Cell(row, 8).Value = sale.TaxAmount;
                ws.Cell(row, 9).Value = sale.TotalAmount;
                ws.Cell(row, 10).Value = sale.AmountPaid;
                ws.Cell(row, 11).Value = sale.ChangeAmount;
                ws.Cell(row, 12).Value = sale.Status;
                row++;
            }

            FormatMoneyColumns(ws, currency, 6, 11);
            StyleHeader(ws, "A1:L1");
            StyleUsedRange(ws);
        }

        private static void BuildRefundsSheet(XLWorkbook workbook, List<SaleReturn> refunds, string currency)
        {
            var ws = workbook.Worksheets.Add("Refunds");

            var headers = new[]
            {
                "Return No", "Sale No", "Date", "Type", "Items", "Refund Amount", "Status"
            };

            WriteHeaders(ws, headers);

            var row = 2;

            foreach (var refund in refunds)
            {
                ws.Cell(row, 1).Value = refund.ReturnNumber;
                ws.Cell(row, 2).Value = refund.Sale?.SaleNumber ?? "-";
                ws.Cell(row, 3).Value = refund.CreatedAt;
                ws.Cell(row, 4).Value = refund.ReturnType;
                ws.Cell(row, 5).Value = refund.SaleReturnItems.Count;
                ws.Cell(row, 6).Value = refund.TotalRefundAmount;
                ws.Cell(row, 7).Value = refund.Status;
                row++;
            }

            FormatMoneyColumns(ws, currency, 6, 6);
            StyleHeader(ws, "A1:G1");
            StyleUsedRange(ws);
        }

        private static void BuildExpensesSheet(XLWorkbook workbook, List<Expense> expenses, string currency)
        {
            var ws = workbook.Worksheets.Add("Expenses");

            var headers = new[]
            {
                "Expense No", "Date", "Category", "Vendor", "Reference",
                "Payment", "Subtotal", "Tax", "Total", "Created By", "Notes"
            };

            WriteHeaders(ws, headers);

            var row = 2;

            foreach (var expense in expenses)
            {
                ws.Cell(row, 1).Value = expense.ExpenseNumber;
                ws.Cell(row, 2).Value = expense.ExpenseDate;
                ws.Cell(row, 3).Value = expense.ExpenseCategory?.CategoryName ?? "-";
                ws.Cell(row, 4).Value = expense.VendorName ?? "-";
                ws.Cell(row, 5).Value = expense.ReferenceNumber ?? "-";
                ws.Cell(row, 6).Value = expense.PaymentMethod;
                ws.Cell(row, 7).Value = expense.Subtotal;
                ws.Cell(row, 8).Value = expense.TaxAmount;
                ws.Cell(row, 9).Value = expense.TotalAmount;
                ws.Cell(row, 10).Value = expense.CreatedByName ?? "-";
                ws.Cell(row, 11).Value = expense.Notes ?? "";
                row++;
            }

            FormatMoneyColumns(ws, currency, 7, 9);
            StyleHeader(ws, "A1:K1");
            StyleUsedRange(ws);
        }

        private static void BuildPurchasesSheet(XLWorkbook workbook, List<StockPurchase> purchases, string currency)
        {
            var ws = workbook.Worksheets.Add("Purchases");

            var headers = new[]
            {
                "Purchase No", "Date", "Supplier", "Invoice", "Items", "Qty",
                "Subtotal", "Tax", "Total", "Status", "Created By"
            };

            WriteHeaders(ws, headers);

            var row = 2;

            foreach (var purchase in purchases)
            {
                ws.Cell(row, 1).Value = purchase.PurchaseNumber;
                ws.Cell(row, 2).Value = purchase.PurchaseDate;
                ws.Cell(row, 3).Value = purchase.Supplier?.SupplierName ?? "-";
                ws.Cell(row, 4).Value = purchase.SupplierInvoiceNumber ?? "-";
                ws.Cell(row, 5).Value = purchase.StockPurchaseItems.Count;
                decimal totalQuantity = 0;

                foreach (var purchaseItem in purchase.StockPurchaseItems)
                {
                    totalQuantity += purchaseItem.Quantity;
                }

                ws.Cell(row, 6).Value = totalQuantity;
                ws.Cell(row, 7).Value = purchase.Subtotal;
                ws.Cell(row, 8).Value = purchase.TaxAmount;
                ws.Cell(row, 9).Value = purchase.TotalAmount;
                ws.Cell(row, 10).Value = purchase.Status;
                ws.Cell(row, 11).Value = purchase.CreatedByName ?? "-";
                row++;
            }

            FormatMoneyColumns(ws, currency, 7, 9);
            StyleHeader(ws, "A1:K1");
            StyleUsedRange(ws);
        }

        private static void BuildStockSheet(XLWorkbook workbook, List<Product> stock, string currency)
        {
            var ws = workbook.Worksheets.Add("Stock");

            var headers = new[]
            {
                "Product", "SKU", "Barcode", "Category", "Type", "Available",
                "Reorder", "Cost", "Selling", "Stock Value", "Status"
            };

            WriteHeaders(ws, headers);

            var row = 2;

            foreach (var product in stock)
            {
                var stockValue = product.QuantityInStock * product.CostPrice;

                ws.Cell(row, 1).Value = product.ProductName;
                ws.Cell(row, 2).Value = product.SKU;
                ws.Cell(row, 3).Value = product.Barcode ?? "-";
                ws.Cell(row, 4).Value = product.ProductCategory?.Name ?? "-";
                ws.Cell(row, 5).Value = product.ProductType;
                ws.Cell(row, 6).Value = product.QuantityInStock;
                ws.Cell(row, 7).Value = product.ReorderLevel;
                ws.Cell(row, 8).Value = product.CostPrice;
                ws.Cell(row, 9).Value = product.SellingPrice;
                ws.Cell(row, 10).Value = stockValue;
                ws.Cell(row, 11).Value = product.IsActive ? "Active" : "Inactive";
                row++;
            }

            FormatMoneyColumns(ws, currency, 8, 10);
            StyleHeader(ws, "A1:K1");
            StyleUsedRange(ws);
        }

        private static void WriteHeaders(IXLWorksheet ws, string[] headers)
        {
            for (var i = 0; i < headers.Length; i++)
            {
                ws.Cell(1, i + 1).Value = headers[i];
            }
        }

        private static void StyleTitle(IXLWorksheet ws, string range)
        {
            var titleRange = ws.Range(range);
            titleRange.Style.Font.Bold = true;
            titleRange.Style.Font.FontSize = 14;
            titleRange.Style.Font.FontColor = XLColor.White;
            titleRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#111827");
        }

        private static void StyleHeader(IXLWorksheet ws, string range)
        {
            var header = ws.Range(range);
            header.Style.Font.Bold = true;
            header.Style.Font.FontColor = XLColor.White;
            header.Style.Fill.BackgroundColor = XLColor.FromHtml("#111827");
            header.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        private static void StyleUsedRange(IXLWorksheet ws)
        {
            var usedRange = ws.RangeUsed();

            if (usedRange == null)
            {
                return;
            }

            usedRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            usedRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            usedRange.Style.Border.OutsideBorderColor = XLColor.FromHtml("#e5e7eb");
            usedRange.Style.Border.InsideBorderColor = XLColor.FromHtml("#e5e7eb");

            ws.Columns().AdjustToContents();

            foreach (var column in ws.ColumnsUsed())
            {
                if (column.Width > 34)
                {
                    column.Width = 34;
                    column.Style.Alignment.WrapText = true;
                }
            }

            ws.SheetView.FreezeRows(1);
        }

        private static void FormatMoneyColumns(IXLWorksheet ws, string currency, int fromColumn, int toColumn)
        {
            for (var col = fromColumn; col <= toColumn; col++)
            {
                ws.Column(col).Style.NumberFormat.Format = $"{currency} #,##0.00";
            }
        }
    }
}