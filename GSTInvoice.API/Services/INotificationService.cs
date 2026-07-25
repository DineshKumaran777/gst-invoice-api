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
using GSTInvoice.Shared.DTOs.Notification;
using GSTInvoice.Shared.Enums;
using GSTInvoice.Shared.Pagination;

namespace GSTInvoice.API.Services;

public interface INotificationService
{
    Task NotifyTenantAsync(Guid tenantId, string title, string message, NotificationType type, CancellationToken cancellationToken = default);

    Task<PagedResult<NotificationDto>> GetMyNotificationsAsync(PaginationRequest request, CancellationToken cancellationToken = default);

    Task<int> GetUnreadCountAsync(CancellationToken cancellationToken = default);

    Task MarkAsReadAsync(Guid notificationId, CancellationToken cancellationToken = default);
}

