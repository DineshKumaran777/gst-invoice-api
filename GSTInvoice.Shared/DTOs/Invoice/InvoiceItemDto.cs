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

public class InvoiceItemDto
{
    public Guid Id { get; set; }

    public Guid? ProductId { get; set; }

    public string Description { get; set; } = string.Empty;

    public string HSNCode { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    public string Unit { get; set; } = "Nos";

    public decimal UnitPrice { get; set; }

    public decimal DiscountPercentage { get; set; }

    public GSTRate GSTRate { get; set; }

    public decimal CGSTAmount { get; set; }

    public decimal SGSTAmount { get; set; }

    public decimal IGSTAmount { get; set; }

    public decimal TotalAmount { get; set; }
}

