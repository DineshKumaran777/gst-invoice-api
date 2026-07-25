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
using GSTInvoice.Shared.DTOs.Auth;
using GSTInvoice.Shared.Enums;
using GSTInvoice.Shared.Pagination;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GSTInvoice.API.Services;

public class UserService(
    UserManager<ApplicationUser> userManager,
    ITenantContextAccessor tenantContextAccessor,
    AppDbContext dbContext)
    : IUserService
{
    public async Task<PagedResult<UserSummaryDto>> GetUsersAsync(PaginationRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContextAccessor.GetTenantId();

        var query = userManager.Users
            .AsNoTracking()
            .Where(user => user.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(user => user.FullName.Contains(term) || (user.Email != null && user.Email.Contains(term)));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(user => user.FullName)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(user => new UserSummaryDto
            {
                UserId = user.Id,
                TenantId = user.TenantId,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                Role = user.Role,
                IsActive = user.IsActive,
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<UserSummaryDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
        };
    }

    public async Task<UserSummaryDto> InviteAsync(InviteUserRequestDto request, CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContextAccessor.GetTenantId();

        if (request.Role is UserRole.SuperAdmin)
        {
            throw new InvalidOperationException("SuperAdmin role cannot be assigned at tenant scope.");
        }

        var tenant = await dbContext.Tenants.AsNoTracking().FirstOrDefaultAsync(entity => entity.Id == tenantId, cancellationToken)
            ?? throw new InvalidOperationException("Tenant not found.");

        var activePlanName = NormalizePlanName(tenant.SubscriptionPlan);
        var plan = await dbContext.SubscriptionPlans
            .AsNoTracking()
            .FirstOrDefaultAsync(entity => entity.IsActive && entity.Name == activePlanName, cancellationToken);

        if (plan is not null && plan.MaxUsers > 0 && plan.MaxUsers < int.MaxValue)
        {
            var activeUsersCount = await userManager.Users
                .AsNoTracking()
                .CountAsync(entity => entity.TenantId == tenantId && entity.IsActive, cancellationToken);

            if (activeUsersCount >= plan.MaxUsers)
            {
                throw new InvalidOperationException($"User limit reached for the {plan.Name} plan. Upgrade your subscription to add more users.");
            }
        }

        var email = request.Email.Trim().ToLowerInvariant();
        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null)
        {
            throw new InvalidOperationException("A user with this email already exists.");
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FullName = request.FullName.Trim(),
            TenantId = tenantId,
            Role = request.Role,
            IsActive = true,
        };

        var result = await userManager.CreateAsync(user, request.TemporaryPassword.Trim());
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(error => error.Description));
            throw new InvalidOperationException($"Failed to invite user: {errors}");
        }

        await userManager.AddToRoleAsync(user, request.Role.ToString());

        return new UserSummaryDto
        {
            UserId = user.Id,
            TenantId = user.TenantId,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            Role = user.Role,
            IsActive = user.IsActive,
        };
    }

    public async Task<bool> DeactivateAsync(string userId, CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContextAccessor.GetTenantId();
        var currentUserId = tenantContextAccessor.GetUserId();

        if (string.Equals(currentUserId, userId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("You cannot deactivate your own account.");
        }

        var user = await userManager.Users.FirstOrDefaultAsync(entity => entity.Id == userId && entity.TenantId == tenantId, cancellationToken);
        if (user is null)
        {
            return false;
        }

        if (user.Role == UserRole.CompanyAdmin)
        {
            var activeCompanyAdmins = await userManager.Users
                .AsNoTracking()
                .CountAsync(
                    entity => entity.TenantId == tenantId
                        && entity.IsActive
                        && entity.Role == UserRole.CompanyAdmin,
                    cancellationToken);

            if (activeCompanyAdmins <= 1)
            {
                throw new InvalidOperationException("At least one active CompanyAdmin is required.");
            }
        }

        user.IsActive = false;
        await userManager.UpdateAsync(user);
        return true;
    }

    private static string NormalizePlanName(string subscriptionPlan)
    {
        var openingIndex = subscriptionPlan.IndexOf('(');
        if (openingIndex <= 0)
        {
            return subscriptionPlan.Trim();
        }

        return subscriptionPlan[..openingIndex].Trim();
    }
}

