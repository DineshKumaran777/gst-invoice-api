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
using System.ComponentModel.DataAnnotations;
using GSTInvoice.Shared.Enums;

namespace GSTInvoice.API.Models;

public class Invoice
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TenantId { get; set; }

    public Tenant? Tenant { get; set; }

    public Guid ClientId { get; set; }

    public Client? Client { get; set; }

    [Required]
    [MaxLength(450)]
    public string CreatedByUserId { get; set; } = string.Empty;

    public ApplicationUser? CreatedByUser { get; set; }

    [Required]
    [MaxLength(50)]
    public string InvoiceNumber { get; set; } = string.Empty;

    public InvoiceType InvoiceType { get; set; } = InvoiceType.TaxInvoice;

    public DateTime InvoiceDate { get; set; }

    public DateTime DueDate { get; set; }

    [Required]
    [MaxLength(100)]
    public string PlaceOfSupply { get; set; } = string.Empty;

    [MaxLength(60)]
    public string? PONumber { get; set; }

    [MaxLength(60)]
    public string? ReferenceNumber { get; set; }

    public decimal Subtotal { get; set; }

    public decimal Discount { get; set; }

    public decimal TaxableAmount { get; set; }

    public decimal TotalCGST { get; set; }

    public decimal TotalSGST { get; set; }

    public decimal TotalIGST { get; set; }

    public decimal GrandTotal { get; set; }

    public decimal RoundOff { get; set; }

    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;

    [MaxLength(1500)]
    public string? Notes { get; set; }

    [MaxLength(2000)]
    public string? Terms { get; set; }

    [MaxLength(100)]
    public string? EmailStatus { get; set; }

    [MaxLength(100)]
    public string? SmsStatus { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<InvoiceItem> Items { get; set; } = [];

    public ICollection<Payment> Payments { get; set; } = [];
}

