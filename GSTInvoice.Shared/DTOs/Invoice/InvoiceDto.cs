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
using GSTInvoice.Shared.Enums;

namespace GSTInvoice.Shared.DTOs.Invoice;

public class InvoiceDto
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public Guid ClientId { get; set; }

    public string ClientName { get; set; } = string.Empty;

    public string? ClientEmail { get; set; }

    public string? ClientPhone { get; set; }

    public string CreatedByUserId { get; set; } = string.Empty;

    public string InvoiceNumber { get; set; } = string.Empty;

    public InvoiceType InvoiceType { get; set; }

    public DateTime InvoiceDate { get; set; }

    public DateTime DueDate { get; set; }

    public string PlaceOfSupply { get; set; } = string.Empty;

    public string? PONumber { get; set; }

    public string? ReferenceNumber { get; set; }

    public decimal Subtotal { get; set; }

    public decimal Discount { get; set; }

    public decimal TaxableAmount { get; set; }

    public decimal TotalCGST { get; set; }

    public decimal TotalSGST { get; set; }

    public decimal TotalIGST { get; set; }

    public decimal GrandTotal { get; set; }

    public decimal RoundOff { get; set; }

    public InvoiceStatus Status { get; set; }

    public string? Notes { get; set; }

    public string? Terms { get; set; }

    public string? EmailStatus { get; set; }

    public string? SmsStatus { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public List<InvoiceItemDto> Items { get; set; } = [];
}

