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

public class AccountingSummaryDto
{
    public DateTime FromDateUtc { get; set; }

    public DateTime ToDateUtc { get; set; }

    public decimal GrossSales { get; set; }

    public decimal CashIn { get; set; }

    public decimal BankIn { get; set; }

    public decimal UpiIn { get; set; }

    public decimal CardIn { get; set; }

    public decimal TotalCollections { get; set; }

    public decimal OutstandingAmount { get; set; }

    public decimal EstimatedExpenses { get; set; }

    public decimal EstimatedNetProfit { get; set; }

    public decimal TrialBalanceDebit { get; set; }

    public decimal TrialBalanceCredit { get; set; }
}
