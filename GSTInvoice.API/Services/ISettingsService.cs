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
using GSTInvoice.Shared.DTOs.Tenant;
using GSTInvoice.Shared.Pagination;
using GSTInvoice.Shared.DTOs.Auth;

namespace GSTInvoice.API.Services;

public interface ISettingsService
{
    Task<CompanySettingsDto> GetCompanySettingsAsync(CancellationToken cancellationToken = default);

    Task<CompanySettingsDto> UpdateCompanySettingsAsync(UpdateCompanySettingsRequestDto request, CancellationToken cancellationToken = default);

    Task<UiPreferencesDto> GetUiPreferencesAsync(CancellationToken cancellationToken = default);

    Task<UiPreferencesDto> UpdateUiPreferencesAsync(UiPreferencesDto request, CancellationToken cancellationToken = default);

    Task<PagedResult<SubscriptionPlanDto>> GetSubscriptionPlansAsync(PaginationRequest request, CancellationToken cancellationToken = default);

    Task<CompanySettingsDto> ChangeSubscriptionPlanAsync(ChangeSubscriptionPlanRequestDto request, CancellationToken cancellationToken = default);

    Task<SecuritySettingsDto> GetSecuritySettingsAsync(CancellationToken cancellationToken = default);

    Task<SecuritySettingsDto> UpdateSecuritySettingsAsync(UpdateSecuritySettingsRequestDto request, CancellationToken cancellationToken = default);
}

