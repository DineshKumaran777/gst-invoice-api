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
using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using GSTInvoice.API.Data;
using GSTInvoice.API.Models;
using GSTInvoice.API.Options;
using GSTInvoice.Shared.Common;
using GSTInvoice.Shared.DTOs.Auth;
using GSTInvoice.Shared.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace GSTInvoice.API.Services;

public class AuthService(
    AppDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    IOptions<JwtOptions> jwtOptionsAccessor,
    IJwtSigningKeyProvider jwtSigningKeyProvider,
    IHttpContextAccessor httpContextAccessor,
    IEmailService emailService,
    ISmsService smsService,
    ITwoFactorService twoFactorService,
    ILogger<AuthService> logger)
    : IAuthService
{
    private readonly JwtOptions jwtOptions = jwtOptionsAccessor.Value;
    private readonly ISmsService smsService = smsService;
    private readonly ITwoFactorService twoFactorService = twoFactorService;
    private static readonly ConcurrentDictionary<string, Queue<DateTime>> FailedLoginAttemptsByIp = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<string, LoginOtpChallenge> LoginOtpByEmail = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<string, PasswordResetOtpChallenge> PasswordResetOtpByEmail = new(StringComparer.OrdinalIgnoreCase);

    private const int LoginOtpExpiryMinutes = 5;
    private const int MaxOtpAttempts = 5;
    private const int MaxOtpResends = 3;
    private const int PasswordResetOtpExpiryMinutes = 10;
    private const int MaxPasswordResetOtpAttempts = 5;

    public async Task<TokenResponseDto?> RegisterAsync(RegisterRequestDto request, CancellationToken cancellationToken = default)
    {
        var existing = await userManager.FindByEmailAsync(request.Email);
        if (existing is not null)
        {
            return null;
        }

        var normalizedGstin = request.GSTIN.Trim().ToUpperInvariant();
        var pan = normalizedGstin.Length >= 15 ? normalizedGstin[2..12] : normalizedGstin;

        var tenant = new Tenant
        {
            Name = request.BusinessName.Trim(),
            GSTIN = normalizedGstin,
            PAN = pan,
            Address = $"{request.BusinessName.Trim()}, {request.State.Trim()}",
            State = request.State.Trim(),
            SubscriptionPlan = "Free",
            OtpLoginRequired = false,
            SessionTimeoutMinutes = 30,
            TrialStartsAtUtc = DateTime.UtcNow.Date,
            TrialEndsAtUtc = DateTime.UtcNow.Date.AddDays(14),
            NextRenewalAtUtc = DateTime.UtcNow.Date.AddMonths(1),
            EstimatedSubscriptionChargeInr = 0,
            BusinessType = request.BusinessType,
            IsActive = true,
        };

        dbContext.Tenants.Add(tenant);
        await dbContext.SaveChangesAsync(cancellationToken);

        var user = new ApplicationUser
        {
            UserName = request.Email.Trim().ToLowerInvariant(),
            Email = request.Email.Trim().ToLowerInvariant(),
            EmailConfirmed = true,
            FullName = request.FullName.Trim(),
            Phone = request.Phone.Trim(),
            TenantId = tenant.Id,
            Role = UserRole.CompanyAdmin,
            IsActive = true,
        };

        var createResult = await userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            var errors = string.Join(", ", createResult.Errors.Select(error => error.Description));
            throw new InvalidOperationException($"Unable to create user: {errors}");
        }

        await userManager.AddToRoleAsync(user, UserRole.CompanyAdmin.ToString());
        return await GenerateTokenResponseAsync(user, cancellationToken: cancellationToken);
    }

    public async Task<TokenResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
    {
        var user = await AuthenticateByPasswordAsync(request.Email, request.Password, cancellationToken);
        var tenant = await dbContext.Tenants.AsNoTracking().FirstOrDefaultAsync(entity => entity.Id == user.TenantId, cancellationToken);

        if (user.TwoFactorEnabled || (tenant?.OtpLoginRequired ?? false))
        {
            // Check if user has TOTP authenticator app configured
            var hasTotp = await HasTotpSecretAsync(user.Id);
            if (hasTotp)
            {
                throw new UnauthorizedAccessException("Two-factor authentication required. Please enter your authenticator app code.");
            }

            throw new InvalidOperationException("OTP verification is required. Request OTP and verify code to login.");
        }

        return await GenerateTokenResponseAsync(user, request.RememberMe, cancellationToken);
    }

    public async Task<RequestLoginOtpResponseDto> RequestLoginOtpAsync(RequestLoginOtpRequestDto request, CancellationToken cancellationToken = default)
    {
        var user = await AuthenticateByPasswordAsync(request.Email, request.Password, cancellationToken);
        var normalizedEmail = NormalizeEmail(request.Email);

        CleanupExpiredOtpChallenges();

        var otpCode = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
        var challenge = new LoginOtpChallenge
        {
            UserId = user.Id,
            OtpHash = HashOtp(normalizedEmail, otpCode),
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(LoginOtpExpiryMinutes),
            AttemptCount = 0,
        };

        LoginOtpByEmail[normalizedEmail] = challenge;

        var sent = await emailService.SendInvoiceEmailAsync(
            tenantId: user.TenantId,
            invoiceId: null,
            toEmail: user.Email ?? normalizedEmail,
            subject: "Your DK GST Billing login OTP",
            body: $"<p>Your secure login OTP is <strong>{otpCode}</strong>.</p><p>This code expires in {LoginOtpExpiryMinutes} minutes.</p><p>If you did not request this OTP, please reset your password immediately.</p>",
            cancellationToken: cancellationToken);

        if (!sent)
        {
            LoginOtpByEmail.TryRemove(normalizedEmail, out _);
            throw new InvalidOperationException("Unable to send OTP. Please verify email configuration and retry.");
        }

        return new RequestLoginOtpResponseDto
        {
            DeliveryChannel = "Email",
            DestinationMasked = MaskEmail(normalizedEmail),
            ExpiresAtUtc = challenge.ExpiresAtUtc,
        };
    }

    public async Task<TokenResponseDto> VerifyLoginOtpAsync(VerifyLoginOtpRequestDto request, CancellationToken cancellationToken = default)
    {
        var user = await AuthenticateByPasswordAsync(request.Email, request.Password, cancellationToken);
        var normalizedEmail = NormalizeEmail(request.Email);

        // Check if user has TOTP authenticator app enabled
        if (user.TwoFactorEnabled)
        {
            var hasTotp = await HasTotpSecretAsync(user.Id);
            if (hasTotp)
            {
                var isValidTotp = await twoFactorService.ValidateTotpCodeAsync(user, request.OtpCode.Trim());
                if (!isValidTotp)
                {
                    throw new UnauthorizedAccessException("Invalid authenticator code. Please try again.");
                }

                logger.LogInformation("Successful TOTP verification for {Email}", normalizedEmail);
                return await GenerateTokenResponseAsync(user, cancellationToken: cancellationToken);
            }
        }

        // Fall back to email OTP verification
        CleanupExpiredOtpChallenges();

        if (!LoginOtpByEmail.TryGetValue(normalizedEmail, out var challenge)
            || !string.Equals(challenge.UserId, user.Id, StringComparison.Ordinal))
        {
            throw new UnauthorizedAccessException("OTP not found. Request a new OTP code.");
        }

        if (challenge.ExpiresAtUtc <= DateTime.UtcNow)
        {
            LoginOtpByEmail.TryRemove(normalizedEmail, out _);
            throw new UnauthorizedAccessException("OTP expired. Request a new OTP code.");
        }

        if (challenge.AttemptCount >= MaxOtpAttempts)
        {
            LoginOtpByEmail.TryRemove(normalizedEmail, out _);
            throw new UnauthorizedAccessException("Too many invalid OTP attempts. Request a new OTP code.");
        }

        var incomingHash = HashOtp(normalizedEmail, request.OtpCode.Trim());
        if (!string.Equals(challenge.OtpHash, incomingHash, StringComparison.Ordinal))
        {
            challenge.AttemptCount += 1;
            throw new UnauthorizedAccessException("Invalid OTP code.");
        }

        LoginOtpByEmail.TryRemove(normalizedEmail, out _);
        return await GenerateTokenResponseAsync(user, cancellationToken: cancellationToken);
    }

    public async Task<RequestPasswordResetResponseDto> RequestPasswordResetAsync(RequestPasswordResetRequestDto request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizeEmail(request.Email);
        CleanupExpiredOtpChallenges();

        var user = await userManager.Users.AsNoTracking().FirstOrDefaultAsync(entity => entity.Email == normalizedEmail, cancellationToken);
        if (user is null || !user.IsActive)
        {
            return new RequestPasswordResetResponseDto();
        }

        var otpCode = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
        var challenge = new PasswordResetOtpChallenge
        {
            UserId = user.Id,
            OtpHash = HashOtp(normalizedEmail, otpCode),
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(PasswordResetOtpExpiryMinutes),
            AttemptCount = 0,
        };

        PasswordResetOtpByEmail[normalizedEmail] = challenge;

        var sent = await emailService.SendInvoiceEmailAsync(
            tenantId: user.TenantId,
            invoiceId: null,
            toEmail: user.Email ?? normalizedEmail,
            subject: "Your DK GST Billing password reset OTP",
            body: $"<p>Your password reset OTP is <strong>{otpCode}</strong>.</p><p>This code expires in {PasswordResetOtpExpiryMinutes} minutes.</p><p>If you did not request this reset, you can safely ignore this email.</p>",
            cancellationToken: cancellationToken);

        if (!sent)
        {
            PasswordResetOtpByEmail.TryRemove(normalizedEmail, out _);
            throw new InvalidOperationException("Unable to send password reset OTP. Please verify email configuration and retry.");
        }

        return new RequestPasswordResetResponseDto
        {
            DestinationMasked = MaskEmail(normalizedEmail),
            ExpiresAtUtc = challenge.ExpiresAtUtc,
        };
    }

    public async Task ResetPasswordAsync(ResetPasswordRequestDto request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizeEmail(request.Email);
        CleanupExpiredOtpChallenges();

        if (!PasswordResetOtpByEmail.TryGetValue(normalizedEmail, out var challenge))
        {
            throw new UnauthorizedAccessException("Password reset OTP not found. Request a new OTP.");
        }

        if (challenge.ExpiresAtUtc <= DateTime.UtcNow)
        {
            PasswordResetOtpByEmail.TryRemove(normalizedEmail, out _);
            throw new UnauthorizedAccessException("Password reset OTP expired. Request a new OTP.");
        }

        if (challenge.AttemptCount >= MaxPasswordResetOtpAttempts)
        {
            PasswordResetOtpByEmail.TryRemove(normalizedEmail, out _);
            throw new UnauthorizedAccessException("Too many invalid OTP attempts. Request a new OTP.");
        }

        var incomingHash = HashOtp(normalizedEmail, request.OtpCode.Trim());
        if (!string.Equals(challenge.OtpHash, incomingHash, StringComparison.Ordinal))
        {
            challenge.AttemptCount += 1;
            throw new UnauthorizedAccessException("Invalid password reset OTP.");
        }

        var user = await userManager.Users.FirstOrDefaultAsync(
            entity => entity.Id == challenge.UserId && entity.Email == normalizedEmail,
            cancellationToken);

        if (user is null || !user.IsActive)
        {
            PasswordResetOtpByEmail.TryRemove(normalizedEmail, out _);
            return;
        }

        var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
        var resetResult = await userManager.ResetPasswordAsync(user, resetToken, request.NewPassword);
        if (!resetResult.Succeeded)
        {
            var errors = string.Join(", ", resetResult.Errors.Select(error => error.Description));
            throw new InvalidOperationException($"Unable to reset password: {errors}");
        }

        user.RefreshToken = null;
        user.RefreshTokenExpiresAtUtc = null;
        await userManager.ResetAccessFailedCountAsync(user);
        await userManager.UpdateAsync(user);

        PasswordResetOtpByEmail.TryRemove(normalizedEmail, out _);
    }

    public async Task<RequestLoginOtpResponseDto> ResendLoginOtpAsync(ResendOtpRequestDto request, CancellationToken cancellationToken = default)
    {
        var user = await AuthenticateByPasswordAsync(request.Email, request.Password, cancellationToken);
        var normalizedEmail = NormalizeEmail(request.Email);

        // Check existing challenge for resend limits
        if (LoginOtpByEmail.TryGetValue(normalizedEmail, out var existingChallenge)
            && string.Equals(existingChallenge.UserId, user.Id, StringComparison.Ordinal))
        {
            if (existingChallenge.ResendCount >= MaxOtpResends)
            {
                LoginOtpByEmail.TryRemove(normalizedEmail, out _);
                throw new InvalidOperationException("Maximum OTP resend limit reached. Please try logging in again.");
            }
        }

        CleanupExpiredOtpChallenges();

        var otpCode = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
        var challenge = new LoginOtpChallenge
        {
            UserId = user.Id,
            OtpHash = HashOtp(normalizedEmail, otpCode),
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(LoginOtpExpiryMinutes),
            AttemptCount = 0,
            ResendCount = (existingChallenge?.ResendCount ?? 0) + 1,
        };

        LoginOtpByEmail[normalizedEmail] = challenge;

        var sent = await emailService.SendInvoiceEmailAsync(
            tenantId: user.TenantId,
            invoiceId: null,
            toEmail: user.Email ?? normalizedEmail,
            subject: "Your DK GST Billing login OTP (Resent)",
            body: $"<p>Your secure login OTP is <strong>{otpCode}</strong>.</p><p>This code expires in {LoginOtpExpiryMinutes} minutes.</p><p>If you did not request this OTP, please reset your password immediately.</p>",
            cancellationToken: cancellationToken);

        if (!sent)
        {
            LoginOtpByEmail.TryRemove(normalizedEmail, out _);
            throw new InvalidOperationException("Unable to send OTP. Please verify email configuration and retry.");
        }

        logger.LogInformation("Login OTP resent for {Email} (resend #{ResendCount})", normalizedEmail, challenge.ResendCount);

        return new RequestLoginOtpResponseDto
        {
            DeliveryChannel = "Email",
            DestinationMasked = MaskEmail(normalizedEmail),
            ExpiresAtUtc = challenge.ExpiresAtUtc,
        };
    }

    public async Task<SetupTotpResponseDto> SetupTotpAsync(SetupTotpRequestDto request, CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var user = await userManager.FindByIdAsync(userId)
            ?? throw new UnauthorizedAccessException("User not found.");

        var result = await twoFactorService.GenerateTotpSecretAsync(user, request.Password);
        return result;
    }

    public async Task VerifyTotpSetupAsync(VerifyTotpSetupRequestDto request, CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var user = await userManager.FindByIdAsync(userId)
            ?? throw new UnauthorizedAccessException("User not found.");

        var verified = await twoFactorService.VerifyTotpSetupAsync(user, request.Code);
        if (!verified)
        {
            throw new UnauthorizedAccessException("Invalid verification code. Please try again.");
        }
    }

    public async Task DisableTotpAsync(CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var user = await userManager.FindByIdAsync(userId)
            ?? throw new UnauthorizedAccessException("User not found.");

        await twoFactorService.DisableTotpAsync(user);
    }

    private void TrackFailedLogin(string ipAddress)
    {
        var now = DateTime.UtcNow;
        var bucket = FailedLoginAttemptsByIp.GetOrAdd(ipAddress, _ => new Queue<DateTime>());

        lock (bucket)
        {
            bucket.Enqueue(now);
            while (bucket.TryPeek(out var timestamp) && now - timestamp > TimeSpan.FromMinutes(5))
            {
                bucket.Dequeue();
            }

            if (bucket.Count > 20)
            {
                logger.LogCritical("Potential brute-force attack detected from {IpAddress}. {AttemptCount} failed logins in 5 minutes.", ipAddress, bucket.Count);
            }
        }
    }

    public async Task<TokenResponseDto> RefreshTokenAsync(RefreshTokenRequestDto request, CancellationToken cancellationToken = default)
    {
        var handler = new JwtSecurityTokenHandler();
        ClaimsPrincipal principal;
        try
        {
            principal = handler.ValidateToken(request.AccessToken, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateIssuerSigningKey = true,
                ValidateLifetime = false,
                ValidIssuer = jwtOptions.Issuer,
                ValidAudience = jwtOptions.Audience,
                IssuerSigningKey = jwtSigningKeyProvider.ValidationKey,
            }, out _);
        }
        catch (Exception)
        {
            throw new UnauthorizedAccessException("Invalid access token.");
        }

        var subject = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
            ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(subject))
        {
            throw new UnauthorizedAccessException("Invalid subject claim.");
        }

        var user = await userManager.FindByIdAsync(subject);
        if (user is null || string.IsNullOrWhiteSpace(user.RefreshToken))
        {
            throw new UnauthorizedAccessException("User not found.");
        }

        if (!string.Equals(user.RefreshToken, request.RefreshToken, StringComparison.Ordinal) ||
            user.RefreshTokenExpiresAtUtc < DateTime.UtcNow)
        {
            throw new UnauthorizedAccessException("Refresh token is invalid or expired.");
        }

        user.RefreshToken = null;
        user.RefreshTokenExpiresAtUtc = null;
        await userManager.UpdateAsync(user);

        return await GenerateTokenResponseAsync(user, cancellationToken: cancellationToken);
    }

private async Task<TokenResponseDto> GenerateTokenResponseAsync(ApplicationUser user, bool rememberMe = false, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var tenant = await dbContext.Tenants.AsNoTracking().FirstOrDefaultAsync(entity => entity.Id == user.TenantId, cancellationToken);
        var sessionTimeoutMinutes = tenant?.SessionTimeoutMinutes is > 0 ? tenant.SessionTimeoutMinutes : 30;

        var accessExpiryMinutes = rememberMe ? jwtOptions.RememberMeAccessTokenExpiryMinutes : jwtOptions.AccessTokenExpiryMinutes;
        var refreshExpiryDays = rememberMe ? jwtOptions.RememberMeRefreshTokenExpiryDays : jwtOptions.RefreshTokenExpiryDays;

        var accessExpires = now.AddMinutes(accessExpiryMinutes);
        var refreshExpires = now.AddDays(refreshExpiryDays);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(AppClaimTypes.TenantId, user.TenantId.ToString()),
            new(AppClaimTypes.UserRole, user.Role.ToString()),
            new(ClaimTypes.Role, user.Role.ToString()),
            new(AppClaimTypes.FullName, user.FullName),
        };

        var credentials = new SigningCredentials(jwtSigningKeyProvider.SigningKey, SecurityAlgorithms.RsaSha256);

        var token = new JwtSecurityToken(
            issuer: jwtOptions.Issuer,
            audience: jwtOptions.Audience,
            claims: claims,
            notBefore: now,
            expires: accessExpires,
            signingCredentials: credentials);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);

        user.RefreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        user.RefreshTokenExpiresAtUtc = refreshExpires;
        await userManager.UpdateAsync(user);

        return new TokenResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = user.RefreshToken,
            AccessTokenExpiresAtUtc = accessExpires,
            RefreshTokenExpiresAtUtc = refreshExpires,
            User = new UserSessionDto
            {
                UserId = user.Id,
                TenantId = user.TenantId,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                Role = user.Role,
                SessionTimeoutMinutes = sessionTimeoutMinutes,
            },
        };
    }

    private async Task<ApplicationUser> AuthenticateByPasswordAsync(string email, string password, CancellationToken cancellationToken)
    {
        var normalizedEmail = NormalizeEmail(email);
        var ipAddress = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        var user = await userManager.Users.FirstOrDefaultAsync(
            entity => entity.Email == normalizedEmail,
            cancellationToken);

        if (user is not null && await userManager.IsLockedOutAsync(user))
        {
            logger.LogWarning("Login blocked due to lockout for {Email} from {IpAddress}", normalizedEmail, ipAddress);
            throw new UnauthorizedAccessException("Account is locked. Please retry after 15 minutes.");
        }

        if (user is null || !await userManager.CheckPasswordAsync(user, password))
        {
            TrackFailedLogin(ipAddress);
            if (user is not null)
            {
                await userManager.AccessFailedAsync(user);
            }

            logger.LogWarning("Failed login attempt for {Email} from {IpAddress}", normalizedEmail, ipAddress);
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        await userManager.ResetAccessFailedCountAsync(user);
        logger.LogInformation("Successful password verification for {Email} from {IpAddress}", normalizedEmail, ipAddress);

        if (!user.IsActive)
        {
            throw new UnauthorizedAccessException("User account is inactive.");
        }

        return user;
    }

    private static string NormalizeEmail(string value)
    {
        return value.Trim().ToLowerInvariant();
    }

    private static string HashOtp(string normalizedEmail, string otpCode)
    {
        var payload = Encoding.UTF8.GetBytes($"{normalizedEmail}|{otpCode}|dk-gst-billing");
        return Convert.ToHexString(SHA256.HashData(payload));
    }

    private static void CleanupExpiredOtpChallenges()
    {
        var utcNow = DateTime.UtcNow;
        foreach (var item in LoginOtpByEmail)
        {
            if (item.Value.ExpiresAtUtc <= utcNow)
            {
                LoginOtpByEmail.TryRemove(item.Key, out _);
            }
        }

        foreach (var item in PasswordResetOtpByEmail)
        {
            if (item.Value.ExpiresAtUtc <= utcNow)
            {
                PasswordResetOtpByEmail.TryRemove(item.Key, out _);
            }
        }
    }

    private static string MaskEmail(string email)
    {
        var atIndex = email.IndexOf('@');
        if (atIndex <= 1)
        {
            return "***";
        }

        return $"{email[0]}***{email[(atIndex - 1)..]}";
    }

    private string GetCurrentUserId()
    {
        var userId = httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? httpContextAccessor.HttpContext?.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new UnauthorizedAccessException("User is not authenticated.");
        }

        return userId;
    }

    private async Task<bool> HasTotpSecretAsync(string userId)
    {
        return await dbContext.OtpRecords
            .AnyAsync(o => o.UserId == userId && o.OtpType == "TotpSecret" && !o.IsUsed);
    }

    private sealed class LoginOtpChallenge
    {
        public string UserId { get; set; } = string.Empty;

        public string OtpHash { get; set; } = string.Empty;

        public DateTime ExpiresAtUtc { get; set; }

        public int AttemptCount { get; set; }

        public int ResendCount { get; set; }
    }

    private sealed class PasswordResetOtpChallenge
    {
        public string UserId { get; set; } = string.Empty;

        public string OtpHash { get; set; } = string.Empty;

        public DateTime ExpiresAtUtc { get; set; }

        public int AttemptCount { get; set; }
    }
}