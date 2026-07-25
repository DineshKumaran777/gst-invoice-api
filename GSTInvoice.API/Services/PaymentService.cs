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
using GSTInvoice.API.Models;
using GSTInvoice.Shared.DTOs.Payment;
using GSTInvoice.Shared.Enums;
using GSTInvoice.Shared.Pagination;
using Microsoft.EntityFrameworkCore;

namespace GSTInvoice.API.Services;

public class PaymentService(AppDbContext dbContext, ITenantContextAccessor tenantContextAccessor, INotificationService notificationService) : IPaymentService
{
    public async Task<PagedResult<PaymentDto>> GetPagedAsync(PaginationRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContextAccessor.GetTenantId();

        var query = dbContext.Payments
            .AsNoTracking()
            .Where(payment => payment.TenantId == tenantId);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(payment => payment.DateUtc)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(payment => new PaymentDto
            {
                Id = payment.Id,
                TenantId = payment.TenantId,
                InvoiceId = payment.InvoiceId,
                Amount = payment.Amount,
                DateUtc = payment.DateUtc,
                Mode = payment.Mode,
                Reference = payment.Reference,
                Notes = payment.Notes,
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<PaymentDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
        };
    }

    public async Task<PaymentDto> RecordAsync(RecordPaymentRequestDto request, CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContextAccessor.GetTenantId();

        var invoice = await dbContext.Invoices
            .Include(entity => entity.Payments)
            .FirstOrDefaultAsync(entity => entity.TenantId == tenantId && entity.Id == request.InvoiceId, cancellationToken)
            ?? throw new InvalidOperationException("Invoice not found.");

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            InvoiceId = request.InvoiceId,
            Amount = request.Amount,
            DateUtc = request.DateUtc,
            Mode = request.Mode,
            Reference = request.Reference,
            Notes = request.Notes,
        };

        dbContext.Payments.Add(payment);
        await dbContext.SaveChangesAsync(cancellationToken);

        var totalPaid = await dbContext.Payments
            .Where(entity => entity.TenantId == tenantId && entity.InvoiceId == request.InvoiceId)
            .SumAsync(entity => entity.Amount, cancellationToken);

        if (totalPaid >= invoice.GrandTotal)
        {
            invoice.Status = InvoiceStatus.Paid;
        }
        else if (totalPaid > 0)
        {
            invoice.Status = InvoiceStatus.PartiallyPaid;
        }

        invoice.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        await notificationService.NotifyTenantAsync(
            tenantId,
            "Payment Recorded",
            $"A payment of {payment.Amount:N2} was recorded for invoice {invoice.InvoiceNumber}.",
            NotificationType.System,
            cancellationToken);

        return new PaymentDto
        {
            Id = payment.Id,
            TenantId = payment.TenantId,
            InvoiceId = payment.InvoiceId,
            Amount = payment.Amount,
            DateUtc = payment.DateUtc,
            Mode = payment.Mode,
            Reference = payment.Reference,
            Notes = payment.Notes,
        };
    }
}

