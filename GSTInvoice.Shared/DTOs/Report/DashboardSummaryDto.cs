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

public class DashboardSummaryDto
{
    public int TotalInvoicesThisMonth { get; set; }

    public decimal TotalRevenueThisMonth { get; set; }

    public decimal PendingAmount { get; set; }

    public int OverdueInvoicesCount { get; set; }

    public IReadOnlyList<RevenueByMonthDto> RevenueByMonth { get; set; } = [];

    public IReadOnlyList<InvoiceStatusCountDto> InvoiceStatusDistribution { get; set; } = [];

    public IReadOnlyList<TopClientRevenueDto> TopClientsByRevenue { get; set; } = [];
}

public class RevenueByMonthDto
{
    public string Month { get; set; } = string.Empty;

    public decimal Revenue { get; set; }
}

public class InvoiceStatusCountDto
{
    public string Status { get; set; } = string.Empty;

    public int Count { get; set; }
}

public class TopClientRevenueDto
{
    public Guid ClientId { get; set; }

    public string ClientName { get; set; } = string.Empty;

    public decimal Revenue { get; set; }
}

