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

namespace GSTInvoice.Shared.DTOs.Payment;

public class RecordPaymentRequestDto
{
    [Required]
    public Guid InvoiceId { get; set; }

    [Range(0.01, 999999999)]
    public decimal Amount { get; set; }

    [Required]
    public DateTime DateUtc { get; set; } = DateTime.UtcNow;

    [Required]
    public PaymentMode Mode { get; set; }

    [MaxLength(100)]
    public string? Reference { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }
}

