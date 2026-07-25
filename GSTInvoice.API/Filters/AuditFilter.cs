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
using GSTInvoice.API.Data;
using GSTInvoice.API.Models;
using GSTInvoice.Shared.Common;
using Microsoft.AspNetCore.Mvc.Filters;

namespace GSTInvoice.API.Filters;

public class AuditFilter(AppDbContext dbContext) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var executed = await next();
        if (executed.Exception is not null || context.HttpContext.User.Identity?.IsAuthenticated != true)
        {
            return;
        }

        if (context.HttpContext.Request.Method is not ("POST" or "PUT" or "PATCH" or "DELETE"))
        {
            return;
        }

        if (!context.HttpContext.Items.TryGetValue(AppClaimTypes.TenantId, out var tenantValue) || tenantValue is not Guid tenantId)
        {
            return;
        }

        var userId = context.HttpContext.User.FindFirst("sub")?.Value;
        var entityId = context.RouteData.Values.TryGetValue("id", out var routeId) ? routeId?.ToString() ?? string.Empty : string.Empty;

        dbContext.AuditLogs.Add(new AuditLog
        {
            TenantId = tenantId,
            UserId = userId,
            Action = context.HttpContext.Request.Method,
            EntityType = context.ActionDescriptor.DisplayName ?? "Unknown",
            EntityId = entityId,
            TimestampUtc = DateTime.UtcNow,
            IPAddress = context.HttpContext.Connection.RemoteIpAddress?.ToString(),
        });

        await dbContext.SaveChangesAsync();
    }
}

