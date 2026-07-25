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
using GSTInvoice.Shared.Enums;

namespace GSTInvoice.API.Services;

public class TenantContextAccessor(IHttpContextAccessor httpContextAccessor) : ITenantContextAccessor
{
    public Guid GetTenantId()
    {
        var httpContext = httpContextAccessor.HttpContext ?? throw new InvalidOperationException("No active HTTP context.");
        if (httpContext.Items.TryGetValue(AppClaimTypes.TenantId, out var itemValue) && itemValue is Guid tenantId)
        {
            return tenantId;
        }

        var claimValue = httpContext.User.FindFirst(AppClaimTypes.TenantId)?.Value;
        if (Guid.TryParse(claimValue, out tenantId))
        {
            return tenantId;
        }

        throw new UnauthorizedAccessException("Tenant context not found.");
    }

    public string GetUserId()
    {
        var httpContext = httpContextAccessor.HttpContext ?? throw new InvalidOperationException("No active HTTP context.");
        var userId = httpContext.User.FindFirst("sub")?.Value
            ?? httpContext.User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;

        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new UnauthorizedAccessException("User id not found.");
        }

        return userId;
    }

    public UserRole GetUserRole()
    {
        var httpContext = httpContextAccessor.HttpContext ?? throw new InvalidOperationException("No active HTTP context.");
        var roleText = httpContext.User.FindFirst(AppClaimTypes.UserRole)?.Value;

        return Enum.TryParse<UserRole>(roleText, ignoreCase: true, out var role)
            ? role
            : UserRole.Viewer;
    }
}

