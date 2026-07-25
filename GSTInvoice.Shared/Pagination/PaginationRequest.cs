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
using GSTInvoice.Shared.Common;

namespace GSTInvoice.Shared.Pagination;

public class PaginationRequest
{
    private int pageNumber = AppConstants.DefaultPageNumber;
    private int pageSize = AppConstants.DefaultPageSize;

    public int PageNumber
    {
        get => pageNumber;
        set => pageNumber = value < 1 ? AppConstants.DefaultPageNumber : value;
    }

    public int PageSize
    {
        get => pageSize;
        set => pageSize = value is < 1 or > AppConstants.MaxPageSize
            ? AppConstants.DefaultPageSize
            : value;
    }

    public string? Search { get; set; }

    public string? SortBy { get; set; }

    public bool Descending { get; set; }
}

