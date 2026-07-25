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
using GSTInvoice.Shared.DTOs.Client;
using GSTInvoice.Shared.DTOs.Tenant;

namespace GSTInvoice.Shared.DTOs.Invoice;

public class InvoiceDetailsDto
{
    public InvoiceDto Invoice { get; set; } = new();

    public CompanySettingsDto Seller { get; set; } = new();

    public ClientDto Client { get; set; } = new();

    public bool IsSameState { get; set; }

    public string AmountInWords { get; set; } = string.Empty;

    public string TemplateName { get; set; } = "Default";

    public string BrandHexColor { get; set; } = "#0a84ff";

    public string PdfUrl { get; set; } = string.Empty;
}

