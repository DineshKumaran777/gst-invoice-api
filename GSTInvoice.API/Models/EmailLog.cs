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

namespace GSTInvoice.API.Models;

public class EmailLog
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TenantId { get; set; }

    public Guid? InvoiceId { get; set; }

    public Invoice? Invoice { get; set; }

    [Required]
    [MaxLength(256)]
    public string ToEmail { get; set; } = string.Empty;

    [Required]
    [MaxLength(250)]
    public string Subject { get; set; } = string.Empty;

    [Required]
    [MaxLength(40)]
    public string Status { get; set; } = "Sent";

    public DateTime SentAtUtc { get; set; } = DateTime.UtcNow;

    [MaxLength(2000)]
    public string? ErrorMessage { get; set; }
}

