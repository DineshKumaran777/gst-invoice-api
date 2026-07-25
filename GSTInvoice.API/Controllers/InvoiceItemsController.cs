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
using GSTInvoice.API.Services;
using GSTInvoice.Shared.DTOs.Invoice;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GSTInvoice.API.Controllers;

[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/invoice-items")]
public class InvoiceItemsController(AppDbContext dbContext, ITenantContextAccessor tenantContextAccessor) : ControllerBase
{
    [HttpGet("invoice/{invoiceId:guid}")]
    public async Task<ActionResult<IReadOnlyList<InvoiceItemDto>>> GetByInvoice(Guid invoiceId, CancellationToken cancellationToken)
    {
        var tenantId = tenantContextAccessor.GetTenantId();

        var items = await dbContext.InvoiceItems
            .AsNoTracking()
            .Where(item => item.InvoiceId == invoiceId && item.Invoice != null && item.Invoice.TenantId == tenantId)
            .Select(item => new InvoiceItemDto
            {
                Id = item.Id,
                ProductId = item.ProductId,
                Description = item.Description,
                HSNCode = item.HSNCode,
                Quantity = item.Quantity,
                Unit = item.Unit,
                UnitPrice = item.UnitPrice,
                DiscountPercentage = item.Discount,
                GSTRate = item.GSTRate,
                CGSTAmount = item.CGSTAmount,
                SGSTAmount = item.SGSTAmount,
                IGSTAmount = item.IGSTAmount,
                TotalAmount = item.TotalAmount,
            })
            .ToListAsync(cancellationToken);

        return Ok(items);
    }
}

