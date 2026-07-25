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

namespace GSTInvoice.Shared.DTOs.Invoice;

public class SendInvoiceEmailRequestDto
{
    [EmailAddress]
    [MaxLength(256)]
    public string? ToEmail { get; set; }

    [MaxLength(250)]
    public string? Subject { get; set; }

    [MaxLength(2000)]
    public string? Message { get; set; }

    [EmailAddress]
    [MaxLength(256)]
    public string? Cc { get; set; }

    [EmailAddress]
    [MaxLength(256)]
    public string? Bcc { get; set; }
}

