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

namespace GSTInvoice.Shared.DTOs.Tenant;

public class TenantDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string GSTIN { get; set; } = string.Empty;

    public string PAN { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public string State { get; set; } = string.Empty;

    public string? LogoUrl { get; set; }

    public string SubscriptionPlan { get; set; } = "Free";

    public bool IsActive { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public BusinessType BusinessType { get; set; }
}

