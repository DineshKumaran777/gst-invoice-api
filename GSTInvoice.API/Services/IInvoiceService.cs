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
using GSTInvoice.Shared.DTOs.Invoice;
using GSTInvoice.Shared.Pagination;

namespace GSTInvoice.API.Services;

public interface IInvoiceService
{
    Task<PagedResult<InvoiceDto>> GetPagedAsync(PaginationRequest request, CancellationToken cancellationToken = default);

    Task<InvoiceDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<InvoiceDetailsDto?> GetDetailsAsync(Guid id, CancellationToken cancellationToken = default);

    Task<byte[]?> GeneratePdfAsync(Guid id, CancellationToken cancellationToken = default);

    Task<InvoiceDto> CreateAsync(CreateInvoiceRequestDto request, CancellationToken cancellationToken = default);

    Task<InvoiceDto?> UpdateStatusAsync(Guid id, UpdateInvoiceStatusRequestDto request, CancellationToken cancellationToken = default);

    Task<bool> SendEmailAsync(Guid id, SendInvoiceEmailRequestDto request, CancellationToken cancellationToken = default);

    Task<bool> SendWhatsAppAsync(Guid id, SendInvoiceWhatsAppRequestDto request, CancellationToken cancellationToken = default);
}

