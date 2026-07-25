// =============================================================================
// Copyright © 2024 DK (Freelancer)
// All rights reserved.
//
// Product:     DK GST Billing Platform
// Company:     DK (Freelancer)
// Website:     www.dkgstbilling.com
// Email:       support@dkgstbilling.com
//
// NOTICE: All information contained herein is, and remains the property of
// DK (Freelancer). The intellectual and technical
// concepts contained herein are proprietary to DK (Freelancer)
// and may be covered by Indian and International Patents,
// patents in process, and are protected by trade secret or copyright law.
//
// Unauthorized copying, modification, distribution, or use of this software,
// via any medium, is strictly prohibited without the prior written permission
// of DK (Freelancer).
// =============================================================================
using GSTInvoice.API.Data;
using GSTInvoice.Shared.DTOs.Report;
using GSTInvoice.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;
using System.Xml.Linq;

namespace GSTInvoice.API.Services;

public class ReportService(AppDbContext dbContext, ITenantContextAccessor tenantContextAccessor, ICacheService cacheService) : IReportService
{
    public async Task<DashboardSummaryDto> GetDashboardSummaryAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContextAccessor.GetTenantId();
        var cacheKey = $"tenant:{tenantId}:dashboard:summary";
        var cached = await cacheService.GetAsync<DashboardSummaryDto>(cacheKey, cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var invoices = dbContext.Invoices.AsNoTracking().Where(invoice => invoice.TenantId == tenantId);
        var invoicesThisMonth = invoices.Where(invoice => invoice.InvoiceDate >= monthStart);

        var revenueByMonthRaw = await invoices
            .Where(invoice => invoice.InvoiceDate >= monthStart.AddMonths(-11))
            .GroupBy(invoice => new { invoice.InvoiceDate.Year, invoice.InvoiceDate.Month })
            .Select(group => new
            {
                group.Key.Year,
                group.Key.Month,
                Revenue = group.Sum(item => (double?)item.GrandTotal),
            })
            .OrderBy(item => item.Year)
            .ThenBy(item => item.Month)
            .ToListAsync(cancellationToken);

        var lastFiveInvoices = await invoices
            .Include(invoice => invoice.Client)
            .OrderByDescending(invoice => invoice.CreatedAtUtc)
            .Take(5)
            .Select(invoice => new RecentInvoiceDto
            {
                Id = invoice.Id,
                InvoiceNumber = invoice.InvoiceNumber,
                ClientName = invoice.Client != null ? invoice.Client.Name : "",
                InvoiceDate = invoice.InvoiceDate,
                DueDate = invoice.DueDate,
                Status = invoice.Status,
                GrandTotal = invoice.GrandTotal,
            })
            .ToListAsync(cancellationToken);

        var summary = new DashboardSummaryDto
        {
            TotalInvoicesThisMonth = await invoicesThisMonth.CountAsync(cancellationToken),
            // Sum decimals as double in SQL (SQLite limitation) then convert back to decimal
            TotalRevenueThisMonth = Convert.ToDecimal((await invoicesThisMonth.Where(invoice => invoice.Status == InvoiceStatus.Paid).SumAsync(invoice => (double?)invoice.GrandTotal, cancellationToken)) ?? 0),
            PendingAmount = Convert.ToDecimal((await invoices
                .Where(invoice =>
                    invoice.Status == InvoiceStatus.Draft ||
                    invoice.Status == InvoiceStatus.Sent ||
                    invoice.Status == InvoiceStatus.PartiallyPaid)
                .SumAsync(invoice => (double?)invoice.GrandTotal, cancellationToken)) ?? 0),
            OverdueInvoicesCount = await invoices.CountAsync(invoice => invoice.Status == InvoiceStatus.Overdue, cancellationToken),
            RevenueByMonth = revenueByMonthRaw
                .Select(item => new RevenueByMonthDto
                {
                    Month = $"{item.Year}-{item.Month:D2}",
                    Revenue = Convert.ToDecimal(item.Revenue ?? 0),
                })
                .ToList(),
            InvoiceStatusDistribution = await invoices
                .GroupBy(invoice => invoice.Status)
                .Select(group => new InvoiceStatusCountDto
                {
                    Status = group.Key.ToString(),
                    Count = group.Count(),
                })
                .ToListAsync(cancellationToken),
            TopClientsByRevenue = (await invoices
                .Join(dbContext.Clients,
                    invoice => invoice.ClientId,
                    client => client.Id,
                    (invoice, client) => new { invoice, client })
                .Where(item => item.invoice.TenantId == tenantId)
                .GroupBy(item => new { item.client.Id, item.client.Name })
                .Select(group => new
                {
                    ClientId = group.Key.Id,
                    ClientName = group.Key.Name,
                    Revenue = group.Sum(item => (double?)item.invoice.GrandTotal),
                })
                .OrderByDescending(item => item.Revenue)
                .Take(5)
                .ToListAsync(cancellationToken)).Select(item => new TopClientRevenueDto
                {
                    ClientId = item.ClientId,
                    ClientName = item.ClientName,
                    Revenue = Convert.ToDecimal(item.Revenue ?? 0),
                }).ToList(),
            LastFiveInvoices = lastFiveInvoices,
        };

        await cacheService.SetAsync(cacheKey, summary, TimeSpan.FromMinutes(5), cancellationToken);
        return summary;
    }

    public async Task<GstSummaryDto> GetGstSummaryAsync(DateRangeReportRequestDto request, CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContextAccessor.GetTenantId();

        var invoiceItemsQuery = dbContext.InvoiceItems
            .AsNoTracking()
            .Where(item => item.Invoice != null
                && item.Invoice.TenantId == tenantId
                && item.Invoice.InvoiceDate >= request.FromDateUtc
                && item.Invoice.InvoiceDate <= request.ToDateUtc);

        if (request.ClientId.HasValue)
        {
            invoiceItemsQuery = invoiceItemsQuery.Where(item => item.Invoice != null && item.Invoice.ClientId == request.ClientId.Value);
        }

        var bucketsRaw = await invoiceItemsQuery
            .GroupBy(item => item.GSTRate)
            .Select(group => new
            {
                Rate = ((int)group.Key).ToString(),
                TaxableAmount = group.Sum(item => (double?)(item.TotalAmount - item.CGSTAmount - item.SGSTAmount - item.IGSTAmount)),
                CGST = group.Sum(item => (double?)item.CGSTAmount),
                SGST = group.Sum(item => (double?)item.SGSTAmount),
                IGST = group.Sum(item => (double?)item.IGSTAmount),
            })
            .ToListAsync(cancellationToken);

        var buckets = bucketsRaw.Select(b => new GstBucketDto
        {
            Rate = b.Rate,
            TaxableAmount = Convert.ToDecimal(b.TaxableAmount ?? 0),
            CGST = Convert.ToDecimal(b.CGST ?? 0),
            SGST = Convert.ToDecimal(b.SGST ?? 0),
            IGST = Convert.ToDecimal(b.IGST ?? 0),
        }).ToList();

        return new GstSummaryDto
        {
            FromDateUtc = request.FromDateUtc,
            ToDateUtc = request.ToDateUtc,
            Buckets = buckets,
        };
    }

    public async Task<OutstandingReportDto> GetOutstandingReportAsync(DateRangeReportRequestDto request, CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContextAccessor.GetTenantId();
        var now = DateTime.UtcNow.Date;

        var invoicesQuery = dbContext.Invoices
            .AsNoTracking()
            .Where(invoice => invoice.TenantId == tenantId
                && invoice.InvoiceDate >= request.FromDateUtc
                && invoice.InvoiceDate <= request.ToDateUtc
                && invoice.Status != InvoiceStatus.Paid
                && invoice.Status != InvoiceStatus.Cancelled);

        if (request.ClientId.HasValue)
        {
            invoicesQuery = invoicesQuery.Where(invoice => invoice.ClientId == request.ClientId.Value);
        }

        var overdueInvoices = await invoicesQuery
            .Select(invoice => new
            {
                DueDate = invoice.DueDate,
                Amount = invoice.GrandTotal,
            })
            .ToListAsync(cancellationToken);

        var overdueAges = overdueInvoices.Select(invoice => new
        {
            Days = (now - invoice.DueDate).Days,
            invoice.Amount,
        });

        return new OutstandingReportDto
        {
            Current0To30Days = overdueAges.Count(item => item.Days <= 30),
            Days31To60 = overdueAges.Count(item => item.Days > 30 && item.Days <= 60),
            Days60Plus = overdueAges.Count(item => item.Days > 60),
            OutstandingAmount = overdueAges.Sum(item => item.Amount),
        };
    }

    public async Task<AccountingSummaryDto> GetAccountingSummaryAsync(DateRangeReportRequestDto request, CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContextAccessor.GetTenantId();
        var (fromUtc, toUtc) = NormalizeRange(request);

        var invoiceQuery = dbContext.Invoices
            .AsNoTracking()
            .Where(invoice => invoice.TenantId == tenantId && invoice.InvoiceDate >= fromUtc && invoice.InvoiceDate <= toUtc);

        var paymentQuery = dbContext.Payments
            .AsNoTracking()
            .Where(payment => payment.TenantId == tenantId && payment.DateUtc >= fromUtc && payment.DateUtc <= toUtc);

        if (request.ClientId.HasValue)
        {
            invoiceQuery = invoiceQuery.Where(invoice => invoice.ClientId == request.ClientId.Value);
            paymentQuery = paymentQuery.Where(payment => payment.Invoice != null && payment.Invoice.ClientId == request.ClientId.Value);
        }

        var grossSalesDouble = await invoiceQuery.SumAsync(invoice => (double?)invoice.GrandTotal, cancellationToken);
        var totalCollectionsDouble = await paymentQuery.SumAsync(payment => (double?)payment.Amount, cancellationToken);

        var modeSummariesRaw = await paymentQuery
            .GroupBy(payment => payment.Mode)
            .Select(group => new
            {
                Mode = group.Key,
                Amount = group.Sum(item => (double?)item.Amount),
            })
            .ToListAsync(cancellationToken);

        var outstandingAmountDouble = await invoiceQuery
            .Where(invoice => invoice.Status != InvoiceStatus.Paid && invoice.Status != InvoiceStatus.Cancelled)
            .SumAsync(invoice => (double?)invoice.GrandTotal, cancellationToken);

        var grossSales = Convert.ToDecimal(grossSalesDouble ?? 0);
        var totalCollections = Convert.ToDecimal(totalCollectionsDouble ?? 0);
        var modeSummaries = modeSummariesRaw.Select(m => new { m.Mode, Amount = Convert.ToDecimal(m.Amount ?? 0) }).ToList();
        var outstandingAmount = Convert.ToDecimal(outstandingAmountDouble ?? 0);

        var estimatedExpenses = Math.Round(grossSales * 0.15m, 2, MidpointRounding.AwayFromZero);
        var estimatedNetProfit = Math.Round(totalCollections - estimatedExpenses, 2, MidpointRounding.AwayFromZero);

        var debit = Math.Round(estimatedExpenses + outstandingAmount, 2, MidpointRounding.AwayFromZero);
        var credit = Math.Round(totalCollections, 2, MidpointRounding.AwayFromZero);

        return new AccountingSummaryDto
        {
            FromDateUtc = fromUtc,
            ToDateUtc = toUtc,
            GrossSales = grossSales,
            CashIn = modeSummaries.FirstOrDefault(item => item.Mode == PaymentMode.Cash)?.Amount ?? 0,
            BankIn = modeSummaries.FirstOrDefault(item => item.Mode == PaymentMode.BankTransfer)?.Amount ?? 0,
            UpiIn = modeSummaries.FirstOrDefault(item => item.Mode == PaymentMode.UPI)?.Amount ?? 0,
            CardIn = modeSummaries.FirstOrDefault(item => item.Mode == PaymentMode.Card)?.Amount ?? 0,
            TotalCollections = totalCollections,
            OutstandingAmount = outstandingAmount,
            EstimatedExpenses = estimatedExpenses,
            EstimatedNetProfit = estimatedNetProfit,
            TrialBalanceDebit = debit,
            TrialBalanceCredit = credit,
        };
    }

    public async Task<byte[]> ExportInvoicesCsvAsync(DateRangeReportRequestDto request, CancellationToken cancellationToken = default)
    {
        var rows = await GetInvoiceExportRowsAsync(request, cancellationToken);

        var builder = new StringBuilder();
        builder.AppendLine("InvoiceNumber,InvoiceDate,DueDate,ClientName,Status,TaxableAmount,CGST,SGST,IGST,GrandTotal");

        foreach (var row in rows)
        {
            builder.AppendLine(string.Join(",", [
                EscapeCsv(row.InvoiceNumber),
                EscapeCsv(row.InvoiceDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
                EscapeCsv(row.DueDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
                EscapeCsv(row.ClientName),
                EscapeCsv(row.Status),
                EscapeCsv(row.TaxableAmount.ToString("0.00", CultureInfo.InvariantCulture)),
                EscapeCsv(row.Cgst.ToString("0.00", CultureInfo.InvariantCulture)),
                EscapeCsv(row.Sgst.ToString("0.00", CultureInfo.InvariantCulture)),
                EscapeCsv(row.Igst.ToString("0.00", CultureInfo.InvariantCulture)),
                EscapeCsv(row.GrandTotal.ToString("0.00", CultureInfo.InvariantCulture)),
            ]));
        }

        return Encoding.UTF8.GetBytes(builder.ToString());
    }

    public async Task<byte[]> ExportInvoicesExcelXmlAsync(DateRangeReportRequestDto request, CancellationToken cancellationToken = default)
    {
        var rows = await GetInvoiceExportRowsAsync(request, cancellationToken);

        XNamespace ss = "urn:schemas-microsoft-com:office:spreadsheet";

        var worksheetRows = new List<XElement>
        {
            BuildExcelHeaderRow(ss, "Invoice Number", "Invoice Date", "Due Date", "Client", "Status", "Taxable", "CGST", "SGST", "IGST", "Grand Total"),
        };

        worksheetRows.AddRange(rows.Select(row => new XElement(ss + "Row",
            BuildExcelStringCell(ss, row.InvoiceNumber),
            BuildExcelStringCell(ss, row.InvoiceDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
            BuildExcelStringCell(ss, row.DueDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
            BuildExcelStringCell(ss, row.ClientName),
            BuildExcelStringCell(ss, row.Status),
            BuildExcelNumberCell(ss, row.TaxableAmount),
            BuildExcelNumberCell(ss, row.Cgst),
            BuildExcelNumberCell(ss, row.Sgst),
            BuildExcelNumberCell(ss, row.Igst),
            BuildExcelNumberCell(ss, row.GrandTotal))));

        var workbook = new XDocument(
            new XDeclaration("1.0", "utf-8", "yes"),
            new XElement(ss + "Workbook",
                new XAttribute(XNamespace.Xmlns + "ss", ss),
                new XElement(ss + "Worksheet",
                    new XAttribute(ss + "Name", "Invoices"),
                    new XElement(ss + "Table", worksheetRows))));

        return Encoding.UTF8.GetBytes(workbook.ToString(SaveOptions.DisableFormatting));
    }

    public async Task<byte[]> ExportInvoicesTallyXmlAsync(DateRangeReportRequestDto request, CancellationToken cancellationToken = default)
    {
        var rows = await GetInvoiceExportRowsAsync(request, cancellationToken);

        var vouchers = rows.Select(row =>
            $"<TALLYMESSAGE><VOUCHER VCHTYPE=\"Sales\" ACTION=\"Create\"><DATE>{row.InvoiceDate:yyyyMMdd}</DATE><VOUCHERNUMBER>{EscapeXml(row.InvoiceNumber)}</VOUCHERNUMBER><PARTYLEDGERNAME>{EscapeXml(row.ClientName)}</PARTYLEDGERNAME><NARRATION>Invoice {EscapeXml(row.InvoiceNumber)} exported from DK GST Billing</NARRATION><ALLLEDGERENTRIES.LIST><LEDGERNAME>{EscapeXml(row.ClientName)}</LEDGERNAME><ISDEEMEDPOSITIVE>No</ISDEEMEDPOSITIVE><AMOUNT>{row.GrandTotal.ToString("0.00", CultureInfo.InvariantCulture)}</AMOUNT></ALLLEDGERENTRIES.LIST><ALLLEDGERENTRIES.LIST><LEDGERNAME>Sales</LEDGERNAME><ISDEEMEDPOSITIVE>Yes</ISDEEMEDPOSITIVE><AMOUNT>-{row.GrandTotal.ToString("0.00", CultureInfo.InvariantCulture)}</AMOUNT></ALLLEDGERENTRIES.LIST></VOUCHER></TALLYMESSAGE>");

        var xml = $"<ENVELOPE><HEADER><TALLYREQUEST>Import Data</TALLYREQUEST></HEADER><BODY><IMPORTDATA><REQUESTDESC><REPORTNAME>Vouchers</REPORTNAME></REQUESTDESC><REQUESTDATA>{string.Join(string.Empty, vouchers)}</REQUESTDATA></IMPORTDATA></BODY></ENVELOPE>";

        return Encoding.UTF8.GetBytes(xml);
    }

    private async Task<List<InvoiceExportRow>> GetInvoiceExportRowsAsync(DateRangeReportRequestDto request, CancellationToken cancellationToken)
    {
        var tenantId = tenantContextAccessor.GetTenantId();
        var (fromUtc, toUtc) = NormalizeRange(request);

        var query = dbContext.Invoices
            .AsNoTracking()
            .Include(invoice => invoice.Client)
            .Where(invoice => invoice.TenantId == tenantId && invoice.InvoiceDate >= fromUtc && invoice.InvoiceDate <= toUtc);

        if (request.ClientId.HasValue)
        {
            query = query.Where(invoice => invoice.ClientId == request.ClientId.Value);
        }

        return await query
            .OrderBy(invoice => invoice.InvoiceDate)
            .ThenBy(invoice => invoice.InvoiceNumber)
            .Select(invoice => new InvoiceExportRow
            {
                InvoiceNumber = invoice.InvoiceNumber,
                InvoiceDate = invoice.InvoiceDate,
                DueDate = invoice.DueDate,
                ClientName = invoice.Client != null ? invoice.Client.Name : string.Empty,
                Status = invoice.Status.ToString(),
                TaxableAmount = invoice.TaxableAmount,
                Cgst = invoice.TotalCGST,
                Sgst = invoice.TotalSGST,
                Igst = invoice.TotalIGST,
                GrandTotal = invoice.GrandTotal,
            })
            .ToListAsync(cancellationToken);
    }

    private static (DateTime fromUtc, DateTime toUtc) NormalizeRange(DateRangeReportRequestDto request)
    {
        if (request.ToDateUtc < request.FromDateUtc)
        {
            throw new ArgumentException("To date must be greater than or equal to from date.");
        }

        return (request.FromDateUtc, request.ToDateUtc);
    }

    private static string EscapeCsv(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }

    private static string EscapeXml(string value)
    {
        return System.Security.SecurityElement.Escape(value) ?? string.Empty;
    }

    private static XElement BuildExcelHeaderRow(XNamespace ss, params string[] values)
    {
        return new XElement(ss + "Row", values.Select(value => BuildExcelStringCell(ss, value)));
    }

    private static XElement BuildExcelStringCell(XNamespace ss, string value)
    {
        return new XElement(ss + "Cell", new XElement(ss + "Data", new XAttribute(ss + "Type", "String"), value));
    }

    private static XElement BuildExcelNumberCell(XNamespace ss, decimal value)
    {
        return new XElement(ss + "Cell", new XElement(ss + "Data", new XAttribute(ss + "Type", "Number"), value.ToString("0.00", CultureInfo.InvariantCulture)));
    }

    private sealed class InvoiceExportRow
    {
        public string InvoiceNumber { get; set; } = string.Empty;

        public DateTime InvoiceDate { get; set; }

        public DateTime DueDate { get; set; }

        public string ClientName { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public decimal TaxableAmount { get; set; }

        public decimal Cgst { get; set; }

        public decimal Sgst { get; set; }

        public decimal Igst { get; set; }

        public decimal GrandTotal { get; set; }
    }
}

