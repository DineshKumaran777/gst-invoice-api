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
using GSTInvoice.API.Services;
using GSTInvoice.Shared.DTOs.Auth;
using GSTInvoice.Shared.DTOs.Tenant;
using GSTInvoice.Shared.Pagination;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GSTInvoice.API.Controllers;

[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/settings")]
public class SettingsController(ISettingsService settingsService) : ControllerBase
{
    [HttpGet("company")]
    public async Task<ActionResult<CompanySettingsDto>> GetCompany(CancellationToken cancellationToken)
    {
        var result = await settingsService.GetCompanySettingsAsync(cancellationToken);
        return Ok(result);
    }

    [HttpPut("company")]
    [Authorize(Policy = "CompanyAdminOnly")]
    public async Task<ActionResult<CompanySettingsDto>> UpdateCompany(UpdateCompanySettingsRequestDto request, CancellationToken cancellationToken)
    {
        var result = await settingsService.UpdateCompanySettingsAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpGet("ui-preferences")]
    public async Task<ActionResult<UiPreferencesDto>> GetUiPreferences(CancellationToken cancellationToken)
    {
        var result = await settingsService.GetUiPreferencesAsync(cancellationToken);
        return Ok(result);
    }

    [HttpPut("ui-preferences")]
    public async Task<ActionResult<UiPreferencesDto>> UpdateUiPreferences(UiPreferencesDto request, CancellationToken cancellationToken)
    {
        var result = await settingsService.UpdateUiPreferencesAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpGet("subscription-plans")]
    public async Task<ActionResult<PagedResult<SubscriptionPlanDto>>> GetPlans([FromQuery] PaginationRequest request, CancellationToken cancellationToken)
    {
        var result = await settingsService.GetSubscriptionPlansAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("subscription")]
    [Authorize(Policy = "CompanyAdminOnly")]
    public async Task<ActionResult<CompanySettingsDto>> ChangeSubscription(ChangeSubscriptionPlanRequestDto request, CancellationToken cancellationToken)
    {
        var result = await settingsService.ChangeSubscriptionPlanAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpGet("security")]
    public async Task<ActionResult<SecuritySettingsDto>> GetSecurity(CancellationToken cancellationToken)
    {
        var result = await settingsService.GetSecuritySettingsAsync(cancellationToken);
        return Ok(result);
    }

    [HttpPut("security")]
    [Authorize(Policy = "CompanyAdminOnly")]
    public async Task<ActionResult<SecuritySettingsDto>> UpdateSecurity(UpdateSecuritySettingsRequestDto request, CancellationToken cancellationToken)
    {
        var result = await settingsService.UpdateSecuritySettingsAsync(request, cancellationToken);
        return Ok(result);
    }
}

