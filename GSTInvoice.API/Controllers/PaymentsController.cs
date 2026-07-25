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
using GSTInvoice.Shared.DTOs.Payment;
using GSTInvoice.Shared.Pagination;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GSTInvoice.API.Controllers;

[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/payments")]
public class PaymentsController(IPaymentService paymentService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<PaymentDto>>> Get([FromQuery] PaginationRequest request, CancellationToken cancellationToken)
    {
        var result = await paymentService.GetPagedAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<PaymentDto>> Record(RecordPaymentRequestDto request, CancellationToken cancellationToken)
    {
        var item = await paymentService.RecordAsync(request, cancellationToken);
        return Ok(item);
    }
}

