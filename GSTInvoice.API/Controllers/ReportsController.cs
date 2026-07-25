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
using GSTInvoice.API.Services;
using GSTInvoice.Shared.DTOs.Report;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GSTInvoice.API.Controllers;

[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/reports")]
public class ReportsController(IReportService reportService) : ControllerBase
{
    [HttpGet("dashboard")]
    public async Task<ActionResult<DashboardSummaryDto>> Dashboard(CancellationToken cancellationToken)
    {
        var result = await reportService.GetDashboardSummaryAsync(cancellationToken);
        return Ok(result);
    }

    [HttpPost("gst-summary")]
    public async Task<ActionResult<GstSummaryDto>> GstSummary(DateRangeReportRequestDto request, CancellationToken cancellationToken)
    {
        var result = await reportService.GetGstSummaryAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("outstanding")]
    public async Task<ActionResult<OutstandingReportDto>> Outstanding(DateRangeReportRequestDto request, CancellationToken cancellationToken)
    {
        var result = await reportService.GetOutstandingReportAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("accounting-summary")]
    public async Task<ActionResult<AccountingSummaryDto>> AccountingSummary(DateRangeReportRequestDto request, CancellationToken cancellationToken)
    {
        var result = await reportService.GetAccountingSummaryAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("export/csv")]
    public async Task<IActionResult> ExportCsv(DateRangeReportRequestDto request, CancellationToken cancellationToken)
    {
        var payload = await reportService.ExportInvoicesCsvAsync(request, cancellationToken);
        return File(payload, "text/csv", $"invoices-{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    [HttpPost("export/excel")]
    public async Task<IActionResult> ExportExcel(DateRangeReportRequestDto request, CancellationToken cancellationToken)
    {
        var payload = await reportService.ExportInvoicesExcelXmlAsync(request, cancellationToken);
        return File(payload, "application/vnd.ms-excel", $"invoices-{DateTime.UtcNow:yyyyMMdd}.xml");
    }

    [HttpPost("export/tally")]
    public async Task<IActionResult> ExportTally(DateRangeReportRequestDto request, CancellationToken cancellationToken)
    {
        var payload = await reportService.ExportInvoicesTallyXmlAsync(request, cancellationToken);
        return File(payload, "application/xml", $"tally-vouchers-{DateTime.UtcNow:yyyyMMdd}.xml");
    }
}

