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
namespace GSTInvoice.Shared.DTOs.Report;

public class GstSummaryDto
{
    public DateTime FromDateUtc { get; set; }

    public DateTime ToDateUtc { get; set; }

    public IReadOnlyList<GstBucketDto> Buckets { get; set; } = [];
}

public class GstBucketDto
{
    public string Rate { get; set; } = string.Empty;

    public decimal TaxableAmount { get; set; }

    public decimal CGST { get; set; }

    public decimal SGST { get; set; }

    public decimal IGST { get; set; }
}

