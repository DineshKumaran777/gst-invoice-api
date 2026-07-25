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
using GSTInvoice.Shared.DTOs.Auth;
using GSTInvoice.Shared.DTOs.Tenant;
using GSTInvoice.Shared.Pagination;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace GSTInvoice.API.Services;

public class SettingsService(AppDbContext dbContext, ITenantContextAccessor tenantContextAccessor) : ISettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private static readonly Dictionary<string, decimal> CouponDiscountPercentByCode = new(StringComparer.OrdinalIgnoreCase)
    {
        ["WELCOME10"] = 10,
        ["DKGROWTH15"] = 15,
        ["ANNUAL20"] = 20,
    };

    private static readonly Dictionary<string, string> SupportedThemeModes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["system"] = "system",
        ["light"] = "light",
        ["dark"] = "dark",
    };

    private static readonly Dictionary<string, string> SupportedAccents = new(StringComparer.OrdinalIgnoreCase)
    {
        ["blue"] = "blue",
        ["teal"] = "teal",
        ["amber"] = "amber",
        ["green"] = "green",
    };

    private static readonly Dictionary<string, string> SupportedDensities = new(StringComparer.OrdinalIgnoreCase)
    {
        ["comfortable"] = "comfortable",
        ["compact"] = "compact",
    };

    private static readonly Dictionary<string, string> SupportedFontScales = new(StringComparer.OrdinalIgnoreCase)
    {
        ["normal"] = "normal",
        ["large"] = "large",
    };

    private static readonly Dictionary<string, string> SupportedLocales = new(StringComparer.OrdinalIgnoreCase)
    {
        ["en-IN"] = "en-IN",
        ["hi-IN"] = "hi-IN",
        ["ta-IN"] = "ta-IN",
        ["te-IN"] = "te-IN",
    };

    public async Task<CompanySettingsDto> GetCompanySettingsAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContextAccessor.GetTenantId();
        var tenant = await dbContext.Tenants.AsNoTracking().FirstAsync(entity => entity.Id == tenantId, cancellationToken);

        return MapCompanySettings(tenant);
    }

    public async Task<CompanySettingsDto> UpdateCompanySettingsAsync(UpdateCompanySettingsRequestDto request, CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContextAccessor.GetTenantId();
        var tenant = await dbContext.Tenants.FirstAsync(entity => entity.Id == tenantId, cancellationToken);

        tenant.Name = request.Name.Trim();
        tenant.GSTIN = request.GSTIN.Trim().ToUpperInvariant();
        tenant.PAN = request.PAN.Trim().ToUpperInvariant();
        tenant.Address = request.Address.Trim();
        tenant.State = request.State.Trim();
        tenant.LogoUrl = request.LogoUrl;

        await dbContext.SaveChangesAsync(cancellationToken);

        return MapCompanySettings(tenant);
    }

    public async Task<UiPreferencesDto> GetUiPreferencesAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContextAccessor.GetTenantId();
        var tenant = await dbContext.Tenants.AsNoTracking().FirstAsync(entity => entity.Id == tenantId, cancellationToken);

        return ParseUiPreferences(tenant.UiPreferencesJson);
    }

    public async Task<UiPreferencesDto> UpdateUiPreferencesAsync(UiPreferencesDto request, CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContextAccessor.GetTenantId();
        var tenant = await dbContext.Tenants.FirstAsync(entity => entity.Id == tenantId, cancellationToken);

        var normalized = NormalizeUiPreferences(request);
        tenant.UiPreferencesJson = JsonSerializer.Serialize(normalized, JsonOptions);

        await dbContext.SaveChangesAsync(cancellationToken);
        return normalized;
    }

    public async Task<PagedResult<SubscriptionPlanDto>> GetSubscriptionPlansAsync(PaginationRequest request, CancellationToken cancellationToken = default)
    {
        var query = dbContext.SubscriptionPlans.AsNoTracking().Where(plan => plan.IsActive);
        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(plan => (double)plan.PriceInrPerMonth)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(plan => new SubscriptionPlanDto
            {
                Name = plan.Name,
                PriceInrPerMonth = plan.PriceInrPerMonth,
                MaxInvoicesPerMonth = plan.MaxInvoicesPerMonth,
                MaxUsers = plan.MaxUsers,
                Features = plan.Features,
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<SubscriptionPlanDto>
        {
            Items = items,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalCount = totalCount,
        };
    }

    public async Task<CompanySettingsDto> ChangeSubscriptionPlanAsync(ChangeSubscriptionPlanRequestDto request, CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContextAccessor.GetTenantId();
        var tenant = await dbContext.Tenants.FirstAsync(entity => entity.Id == tenantId, cancellationToken);

        var normalizedPlan = request.PlanName.Trim();
        var plan = await dbContext.SubscriptionPlans
            .AsNoTracking()
            .FirstOrDefaultAsync(entity => entity.IsActive && entity.Name == normalizedPlan, cancellationToken);

        if (plan is null)
        {
            throw new InvalidOperationException("Selected subscription plan is not available.");
        }

        var cycle = string.Equals(request.BillingCycle, "Yearly", StringComparison.OrdinalIgnoreCase)
            ? "Yearly"
            : "Monthly";

        var baseCharge = cycle == "Yearly"
            ? plan.PriceInrPerMonth * 12
            : plan.PriceInrPerMonth;

        if (cycle == "Yearly")
        {
            baseCharge *= 0.90m;
        }

        var normalizedCouponCode = string.IsNullOrWhiteSpace(request.CouponCode)
            ? null
            : request.CouponCode.Trim().ToUpperInvariant();

        decimal discountPercent = 0;
        if (!string.IsNullOrWhiteSpace(normalizedCouponCode))
        {
            if (!CouponDiscountPercentByCode.TryGetValue(normalizedCouponCode, out discountPercent))
            {
                throw new InvalidOperationException("Invalid coupon code.");
            }

            if (string.Equals(normalizedCouponCode, "ANNUAL20", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(cycle, "Yearly", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("ANNUAL20 coupon can be used only for yearly subscriptions.");
            }
        }

        var discountAmount = baseCharge * (discountPercent / 100m);
        var estimatedCharge = Math.Max(0, Math.Round(baseCharge - discountAmount, 2, MidpointRounding.AwayFromZero));

        tenant.SubscriptionPlan = $"{normalizedPlan} ({cycle})";
        tenant.AppliedCouponCode = normalizedCouponCode;
        tenant.EstimatedSubscriptionChargeInr = estimatedCharge;
        tenant.NextRenewalAtUtc = DateTime.UtcNow.Date.AddMonths(cycle == "Yearly" ? 12 : 1);

        if (string.Equals(normalizedPlan, "Free", StringComparison.OrdinalIgnoreCase))
        {
            tenant.TrialStartsAtUtc ??= DateTime.UtcNow.Date;
            tenant.TrialEndsAtUtc ??= DateTime.UtcNow.Date.AddDays(14);
        }
        else if (tenant.TrialEndsAtUtc is not null && tenant.TrialEndsAtUtc.Value > DateTime.UtcNow)
        {
            tenant.TrialEndsAtUtc = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return MapCompanySettings(tenant);
    }

    public async Task<SecuritySettingsDto> GetSecuritySettingsAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContextAccessor.GetTenantId();
        var userId = tenantContextAccessor.GetUserId();

        var tenant = await dbContext.Tenants.AsNoTracking().FirstAsync(entity => entity.Id == tenantId, cancellationToken);
        var user = await dbContext.Users.AsNoTracking().FirstAsync(entity => entity.Id == userId && entity.TenantId == tenantId, cancellationToken);

        return new SecuritySettingsDto
        {
            TwoFactorEnabled = user.TwoFactorEnabled,
            OtpLoginRequired = tenant.OtpLoginRequired,
            SessionTimeoutMinutes = tenant.SessionTimeoutMinutes <= 0 ? 30 : tenant.SessionTimeoutMinutes,
        };
    }

    public async Task<SecuritySettingsDto> UpdateSecuritySettingsAsync(UpdateSecuritySettingsRequestDto request, CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContextAccessor.GetTenantId();
        var userId = tenantContextAccessor.GetUserId();

        var tenant = await dbContext.Tenants.FirstAsync(entity => entity.Id == tenantId, cancellationToken);
        var user = await dbContext.Users.FirstAsync(entity => entity.Id == userId && entity.TenantId == tenantId, cancellationToken);

        tenant.OtpLoginRequired = request.RequireOtpForLogin;
        tenant.SessionTimeoutMinutes = Math.Clamp(request.SessionTimeoutMinutes <= 0 ? 30 : request.SessionTimeoutMinutes, 10, 720);
        user.TwoFactorEnabled = request.EnableTwoFactor;

        await dbContext.SaveChangesAsync(cancellationToken);

        return new SecuritySettingsDto
        {
            TwoFactorEnabled = user.TwoFactorEnabled,
            OtpLoginRequired = tenant.OtpLoginRequired,
            SessionTimeoutMinutes = tenant.SessionTimeoutMinutes,
        };
    }

    private static CompanySettingsDto MapCompanySettings(GSTInvoice.API.Models.Tenant tenant)
    {
        return new CompanySettingsDto
        {
            TenantId = tenant.Id,
            Name = tenant.Name,
            GSTIN = tenant.GSTIN,
            PAN = tenant.PAN,
            Address = tenant.Address,
            State = tenant.State,
            LogoUrl = tenant.LogoUrl,
            SubscriptionPlan = tenant.SubscriptionPlan,
            IsTrialActive = tenant.TrialEndsAtUtc.HasValue && tenant.TrialEndsAtUtc.Value >= DateTime.UtcNow,
            TrialEndsAtUtc = tenant.TrialEndsAtUtc,
            NextRenewalAtUtc = tenant.NextRenewalAtUtc,
            EstimatedSubscriptionChargeInr = tenant.EstimatedSubscriptionChargeInr,
            AppliedCouponCode = tenant.AppliedCouponCode,
        };
    }

    private static UiPreferencesDto ParseUiPreferences(string? rawPreferences)
    {
        if (string.IsNullOrWhiteSpace(rawPreferences))
        {
            return new UiPreferencesDto();
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<UiPreferencesDto>(rawPreferences, JsonOptions);
            return NormalizeUiPreferences(parsed);
        }
        catch
        {
            return new UiPreferencesDto();
        }
    }

    private static UiPreferencesDto NormalizeUiPreferences(UiPreferencesDto? source)
    {
        var candidate = source ?? new UiPreferencesDto();

        return new UiPreferencesDto
        {
            ThemeMode = NormalizeChoice(candidate.ThemeMode, SupportedThemeModes, "system"),
            Accent = NormalizeChoice(candidate.Accent, SupportedAccents, "blue"),
            Density = NormalizeChoice(candidate.Density, SupportedDensities, "comfortable"),
            FontScale = NormalizeChoice(candidate.FontScale, SupportedFontScales, "normal"),
            Locale = NormalizeChoice(candidate.Locale, SupportedLocales, "en-IN"),
            ReducedMotion = candidate.ReducedMotion,
            CompactTables = candidate.CompactTables,
            HighContrast = candidate.HighContrast,
            CollapsedSidebar = candidate.CollapsedSidebar,
        };
    }

    private static string NormalizeChoice(string? value, IReadOnlyDictionary<string, string> options, string fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        var trimmed = value.Trim();
        return options.TryGetValue(trimmed, out var normalized)
            ? normalized
            : fallback;
    }
}

