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

public class Tenant
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [RegularExpression("^[0-9A-Z]{15}$")]
    public string GSTIN { get; set; } = string.Empty;

    [Required]
    [RegularExpression("^[A-Z]{5}[0-9]{4}[A-Z]$")]
    [MaxLength(10)]
    public string PAN { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string Address { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string State { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? LogoUrl { get; set; }

    [Required]
    [MaxLength(50)]
    public string SubscriptionPlan { get; set; } = "Free";

    public bool OtpLoginRequired { get; set; }

    public int SessionTimeoutMinutes { get; set; } = 30;

    public DateTime? TrialStartsAtUtc { get; set; }

    public DateTime? TrialEndsAtUtc { get; set; }

    public DateTime? NextRenewalAtUtc { get; set; }

    public decimal? EstimatedSubscriptionChargeInr { get; set; }

    [MaxLength(40)]
    public string? AppliedCouponCode { get; set; }

    [MaxLength(4000)]
    public string? UiPreferencesJson { get; set; }

    public BusinessType BusinessType { get; set; } = BusinessType.Company;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public bool IsActive { get; set; } = true;

    public ICollection<ApplicationUser> Users { get; set; } = [];

    public ICollection<Client> Clients { get; set; } = [];

    public ICollection<Product> Products { get; set; } = [];

    public ICollection<Invoice> Invoices { get; set; } = [];
}

