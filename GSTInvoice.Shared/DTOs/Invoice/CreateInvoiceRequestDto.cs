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

namespace GSTInvoice.Shared.DTOs.Invoice;

public class CreateInvoiceRequestDto
{
    [Required]
    public Guid ClientId { get; set; }

    [Required]
    [MaxLength(50)]
    public string InvoiceNumber { get; set; } = string.Empty;

    [Required]
    public InvoiceType InvoiceType { get; set; }

    [Required]
    public DateTime InvoiceDate { get; set; }

    [Required]
    public DateTime DueDate { get; set; }

    [Required]
    [MaxLength(100)]
    public string PlaceOfSupply { get; set; } = string.Empty;

    [MaxLength(60)]
    public string? PONumber { get; set; }

    [MaxLength(60)]
    public string? ReferenceNumber { get; set; }

    [MaxLength(1500)]
    public string? Notes { get; set; }

    [MaxLength(2000)]
    public string? Terms { get; set; }

    [Required]
    [MinLength(1)]
    public List<CreateInvoiceItemRequestDto> Items { get; set; } = [];
}

public class CreateInvoiceItemRequestDto
{
    public Guid? ProductId { get; set; }

    [Required]
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string HSNCode { get; set; } = string.Empty;

    [Range(0.01, 999999)]
    public decimal Quantity { get; set; }

    [Required]
    [MaxLength(20)]
    public string Unit { get; set; } = "Nos";

    [Range(0, 999999999)]
    public decimal UnitPrice { get; set; }

    [Range(0, 100)]
    public decimal DiscountPercentage { get; set; }

    public GSTRate GSTRate { get; set; }
}

