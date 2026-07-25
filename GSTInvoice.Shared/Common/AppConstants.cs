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
namespace GSTInvoice.Shared.Common;

public static class AppConstants
{
    public const int DefaultPageNumber = 1;
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    public const string TenantHeaderName = "X-Tenant-Id";
    public const string CorrelationIdHeaderName = "X-Correlation-Id";

    public static readonly string[] AllowedInvoiceStatuses =
    [
        "Draft",
        "Sent",
        "PartiallyPaid",
        "Paid",
        "Cancelled",
        "Overdue",
    ];
}

