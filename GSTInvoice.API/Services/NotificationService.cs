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
using GSTInvoice.API.Hubs;
using GSTInvoice.API.Models;
using GSTInvoice.Shared.DTOs.Notification;
using GSTInvoice.Shared.Enums;
using GSTInvoice.Shared.Pagination;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace GSTInvoice.API.Services;

public class NotificationService(
    AppDbContext dbContext,
    ITenantContextAccessor tenantContextAccessor,
    IHubContext<NotificationHub> hubContext)
    : INotificationService
{
    public async Task NotifyTenantAsync(Guid tenantId, string title, string message, NotificationType type, CancellationToken cancellationToken = default)
    {
        var userIds = await dbContext.Users
            .Where(user => user.TenantId == tenantId && user.IsActive)
            .Select(user => user.Id)
            .ToListAsync(cancellationToken);

        if (!userIds.Any())
        {
            return;
        }

        foreach (var userId in userIds)
        {
            dbContext.Notifications.Add(new Notification
            {
                TenantId = tenantId,
                UserId = userId,
                Title = title,
                Message = message,
                Type = type,
                IsRead = false,
                CreatedAtUtc = DateTime.UtcNow,
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        await hubContext.Clients.Group($"tenant:{tenantId}").SendAsync("notify", new
        {
            title,
            message,
            type = type.ToString(),
            createdAtUtc = DateTime.UtcNow,
        }, cancellationToken);
    }

    public async Task<PagedResult<NotificationDto>> GetMyNotificationsAsync(PaginationRequest request, CancellationToken cancellationToken = default)
    {
        var userId = tenantContextAccessor.GetUserId();
        var tenantId = tenantContextAccessor.GetTenantId();

        var query = dbContext.Notifications
            .AsNoTracking()
            .Where(notification => notification.TenantId == tenantId && notification.UserId == userId)
            .OrderByDescending(notification => notification.CreatedAtUtc);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(notification => new NotificationDto
            {
                Id = notification.Id,
                TenantId = notification.TenantId,
                UserId = notification.UserId,
                Title = notification.Title,
                Message = notification.Message,
                Type = notification.Type,
                IsRead = notification.IsRead,
                CreatedAtUtc = notification.CreatedAtUtc,
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<NotificationDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
        };
    }

    public async Task<int> GetUnreadCountAsync(CancellationToken cancellationToken = default)
    {
        var userId = tenantContextAccessor.GetUserId();
        var tenantId = tenantContextAccessor.GetTenantId();
        return await dbContext.Notifications.CountAsync(notification => notification.TenantId == tenantId && notification.UserId == userId && !notification.IsRead, cancellationToken);
    }

    public async Task MarkAsReadAsync(Guid notificationId, CancellationToken cancellationToken = default)
    {
        var userId = tenantContextAccessor.GetUserId();
        var tenantId = tenantContextAccessor.GetTenantId();

        var entity = await dbContext.Notifications.FirstOrDefaultAsync(
            notification => notification.TenantId == tenantId && notification.UserId == userId && notification.Id == notificationId,
            cancellationToken);

        if (entity is null)
        {
            return;
        }

        entity.IsRead = true;
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

