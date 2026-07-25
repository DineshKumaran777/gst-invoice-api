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

public class InvoiceItem
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid InvoiceId { get; set; }

    public Invoice? Invoice { get; set; }

    public Guid? ProductId { get; set; }

    public Product? Product { get; set; }

    [Required]
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string HSNCode { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    [Required]
    [MaxLength(20)]
    public string Unit { get; set; } = "Nos";

    public decimal UnitPrice { get; set; }

    public decimal Discount { get; set; }

    public GSTRate GSTRate { get; set; }

    public decimal CGSTAmount { get; set; }

    public decimal SGSTAmount { get; set; }

    public decimal IGSTAmount { get; set; }

    public decimal TotalAmount { get; set; }
}

