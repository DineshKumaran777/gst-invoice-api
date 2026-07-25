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
using GSTInvoice.Shared.DTOs.Invoice;
using GSTInvoice.Shared.Pagination;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GSTInvoice.API.Controllers;

[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/invoices")]
public class InvoicesController(IInvoiceService invoiceService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<InvoiceDto>>> Get([FromQuery] PaginationRequest request, CancellationToken cancellationToken)
    {
        var result = await invoiceService.GetPagedAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<InvoiceDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var item = await invoiceService.GetByIdAsync(id, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpGet("{id:guid}/details")]
    public async Task<ActionResult<InvoiceDetailsDto>> GetDetails(Guid id, CancellationToken cancellationToken)
    {
        var details = await invoiceService.GetDetailsAsync(id, cancellationToken);
        return details is null ? NotFound() : Ok(details);
    }

    [HttpGet("{id:guid}/pdf")]
    public async Task<IActionResult> GetPdf(Guid id, CancellationToken cancellationToken)
    {
        var pdfBytes = await invoiceService.GeneratePdfAsync(id, cancellationToken);
        if (pdfBytes is null)
        {
            return NotFound();
        }

        return File(pdfBytes, "application/pdf", $"invoice-{id}.pdf");
    }

    [HttpPost]
    public async Task<ActionResult<InvoiceDto>> Create(CreateInvoiceRequestDto request, CancellationToken cancellationToken)
    {
        var item = await invoiceService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = item.Id }, item);
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<InvoiceDto>> UpdateStatus(Guid id, UpdateInvoiceStatusRequestDto request, CancellationToken cancellationToken)
    {
        var item = await invoiceService.UpdateStatusAsync(id, request, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost("{id:guid}/send-email")]
    public async Task<IActionResult> SendEmail(Guid id, SendInvoiceEmailRequestDto request, CancellationToken cancellationToken)
    {
        var sent = await invoiceService.SendEmailAsync(id, request, cancellationToken);
        return sent ? Ok(new { message = "Invoice email sent." }) : BadRequest(new { message = "Unable to send invoice email." });
    }

    [HttpPost("{id:guid}/send-whatsapp")]
    public async Task<IActionResult> SendWhatsApp(Guid id, SendInvoiceWhatsAppRequestDto request, CancellationToken cancellationToken)
    {
        var sent = await invoiceService.SendWhatsAppAsync(id, request, cancellationToken);
        return sent ? Ok(new { message = "WhatsApp notification sent." }) : BadRequest(new { message = "Unable to send WhatsApp notification." });
    }
}

