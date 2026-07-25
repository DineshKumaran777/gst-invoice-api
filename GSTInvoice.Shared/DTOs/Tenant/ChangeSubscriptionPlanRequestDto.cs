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

namespace GSTInvoice.Shared.DTOs.Tenant;

public class ChangeSubscriptionPlanRequestDto
{
    [Required]
    [MaxLength(50)]
    public string PlanName { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string BillingCycle { get; set; } = "Monthly";

    [Required]
    public PaymentMode PaymentMode { get; set; }

    [Required]
    [MaxLength(120)]
    public string PaymentReference { get; set; } = string.Empty;

    [MaxLength(40)]
    public string? CouponCode { get; set; }
}

