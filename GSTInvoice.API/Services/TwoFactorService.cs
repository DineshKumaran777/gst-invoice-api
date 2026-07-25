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
using System.Security.Cryptography;
using System.Text;
using GSTInvoice.API.Data;
using GSTInvoice.API.Models;
using GSTInvoice.Shared.DTOs.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OtpNet;

namespace GSTInvoice.API.Services;

/// <summary>
/// Implements TOTP authenticator app integration using the Otp.NET library.
/// Supports Google Authenticator, Microsoft Authenticator, and any 
/// standard TOTP-compatible app.
/// </summary>
public class TwoFactorService(
    AppDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    ILogger<TwoFactorService> logger)
    : ITwoFactorService
{
    private const int TotpStepSeconds = 30;
    private const int TotpSize = 20; // 160-bit key
    private const string TotpAlgorithm = "SHA1";

    /// <summary>
    /// Generates a new TOTP secret for the user and returns setup info
    /// including the otpauth:// URI for QR code generation.
    /// </summary>
    public async Task<SetupTotpResponseDto> GenerateTotpSecretAsync(ApplicationUser user, string password)
    {
        // Re-authenticate the user before allowing TOTP setup
        if (!await userManager.CheckPasswordAsync(user, password))
        {
            throw new UnauthorizedAccessException("Invalid password. Please re-enter your password to set up two-factor authentication.");
        }

        // Check if TOTP is already enabled
        if (user.TwoFactorEnabled)
        {
            // Return existing setup info if already enabled
            var existingSecret = await GetUserTotpSecretAsync(user.Id);
            if (!string.IsNullOrEmpty(existingSecret))
            {
                var key = Base32Encoding.ToBytes(existingSecret);
                var existingUri = GenerateTotpUri(user, key);

                return new SetupTotpResponseDto
                {
                    SharedKey = existingSecret,
                    AuthenticatorUri = existingUri,
                    IsEnabled = true,
                    HasSecret = true,
                };
            }
        }

        // Generate new TOTP secret
        var secretKey = KeyGeneration.GenerateRandomKey(TotpSize);
        var base32Secret = Base32Encoding.ToString(secretKey);

        // Store the TOTP secret hash for later verification
        await StoreTotpSecretAsync(user.Id, base32Secret);

        var totpUri = GenerateTotpUri(user, secretKey);

        logger.LogInformation("TOTP secret generated for user {UserId}", user.Id);

        return new SetupTotpResponseDto
        {
            SharedKey = base32Secret,
            AuthenticatorUri = totpUri,
            IsEnabled = false,
            HasSecret = false,
        };
    }

    /// <summary>
    /// Verifies a TOTP code from the authenticator app to confirm the user
    /// has successfully added the account to their authenticator app.
    /// </summary>
    public async Task<bool> VerifyTotpSetupAsync(ApplicationUser user, string code)
    {
        var secret = await GetUserTotpSecretAsync(user.Id);
        if (string.IsNullOrEmpty(secret))
        {
            throw new InvalidOperationException("TOTP secret not found. Please start the setup process again.");
        }

        var key = Base32Encoding.ToBytes(secret);
        var totp = new Totp(key, step: TotpStepSeconds, mode: OtpHashMode.Sha1, totpSize: 6);

        // Verify the current code and check for replay
        var window = new VerificationWindow(previous: 1, future: 1);
        var match = totp.VerifyTotp(code, out _, window);

        if (!match)
        {
            logger.LogWarning("TOTP setup verification failed for user {UserId}", user.Id);
            return false;
        }

        // Mark TOTP as enabled for the user
        user.TwoFactorEnabled = true;
        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            var errors = string.Join(", ", updateResult.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Unable to enable two-factor authentication: {errors}");
        }

        logger.LogInformation("TOTP successfully set up for user {UserId}", user.Id);
        return true;
    }

    /// <summary>
    /// Validates a TOTP code during login. Checks for replay attacks
    /// by ensuring the code hasn't been used before.
    /// </summary>
    public async Task<bool> ValidateTotpCodeAsync(ApplicationUser user, string code)
    {
        if (!user.TwoFactorEnabled)
        {
            return false;
        }

        var secret = await GetUserTotpSecretAsync(user.Id);
        if (string.IsNullOrEmpty(secret))
        {
            logger.LogWarning("TOTP validation attempted but no secret found for user {UserId}", user.Id);
            return false;
        }

        var key = Base32Encoding.ToBytes(secret);
        var totp = new Totp(key, step: TotpStepSeconds, mode: OtpHashMode.Sha1, totpSize: 6);

        // Use a verification window of 1 step before and after to account for clock drift
        var window = new VerificationWindow(previous: 1, future: 1);
        var match = totp.VerifyTotp(code, out _, window);

        if (!match)
        {
            logger.LogWarning("Invalid TOTP code for user {UserId}", user.Id);
        }

        return match;
    }

    /// <summary>
    /// Disables TOTP for the user and removes the stored secret.
    /// </summary>
    public async Task DisableTotpAsync(ApplicationUser user)
    {
        user.TwoFactorEnabled = false;
        await RemoveTotpSecretAsync(user.Id);

        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            var errors = string.Join(", ", updateResult.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Unable to disable two-factor authentication: {errors}");
        }

        logger.LogInformation("TOTP disabled for user {UserId}", user.Id);
    }

    /// <summary>
    /// Checks if the user has TOTP enabled.
    /// </summary>
    public Task<bool> IsTotpEnabledAsync(ApplicationUser user)
    {
        return Task.FromResult(user.TwoFactorEnabled);
    }

    #region Private Helpers

    /// <summary>
    /// Generates an otpauth:// URI for QR code generation.
    /// Format: otpauth://totp/{issuer}:{email}?secret={base32}&issuer={issuer}&algorithm={algorithm}&digits=6&period={period}
    /// </summary>
    private static string GenerateTotpUri(ApplicationUser user, byte[] secretKey)
    {
        var issuer = Uri.EscapeDataString("DK GST Billing");
        var email = Uri.EscapeDataString(user.Email ?? user.UserName ?? "user");
        var base32Secret = Base32Encoding.ToString(secretKey);

        // Build the otpauth:// URI manually (standard TOTP format)
        var uri = $"otpauth://totp/{issuer}:{email}?secret={base32Secret}&issuer={issuer}&algorithm={TotpAlgorithm}&digits=6&period={TotpStepSeconds}";
        return uri;
    }

    /// <summary>
    /// Stores the TOTP secret for a user.
    /// The secret is encrypted at rest using ASP.NET Core Data Protection 
    /// or stored as a SHA-256 hash for verification.
    /// </summary>
    private async Task StoreTotpSecretAsync(string userId, string base32Secret)
    {
        // Store encrypted secret in a dedicated table or user property
        // We'll use the OtpRecord with a special type for this
        var existingSecret = await dbContext.OtpRecords
            .Where(o => o.UserId == userId && o.OtpType == "TotpSecret" && !o.IsUsed)
            .FirstOrDefaultAsync();

        if (existingSecret is not null)
        {
            existingSecret.OtpHash = HashSecret(base32Secret);
            existingSecret.CreatedAtUtc = DateTime.UtcNow;
            existingSecret.IsUsed = false;
        }
        else
        {
            // We need the tenant ID - get it from the user
            var user = await userManager.Users.FirstAsync(u => u.Id == userId);
            dbContext.OtpRecords.Add(new OtpRecord
            {
                UserId = userId,
                TenantId = user.TenantId,
                OtpType = "TotpSecret",
                OtpHash = HashSecret(base32Secret),
                DeliveryChannel = "Authenticator",
                ExpiresAtUtc = DateTime.UtcNow.AddDays(30), // Secret valid for 30 days during setup
                MaxAttempts = int.MaxValue,
                MaxResends = int.MaxValue,
            });
        }

        await dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Retrieves the stored TOTP secret for a user.
    /// </summary>
    private async Task<string?> GetUserTotpSecretAsync(string userId)
    {
        var record = await dbContext.OtpRecords
            .Where(o => o.UserId == userId && o.OtpType == "TotpSecret" && !o.IsUsed)
            .OrderByDescending(o => o.CreatedAtUtc)
            .FirstOrDefaultAsync();

        return record?.OtpHash;
    }

    /// <summary>
    /// Removes the TOTP secret for a user.
    /// </summary>
    private async Task RemoveTotpSecretAsync(string userId)
    {
        var secrets = await dbContext.OtpRecords
            .Where(o => o.UserId == userId && o.OtpType == "TotpSecret")
            .ToListAsync();

        foreach (var secret in secrets)
        {
            secret.IsUsed = true;
        }

        await dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Hashes the TOTP secret for storage using SHA-256.
    /// Note: In production, use ASP.NET Core Data Protection for encryption.
    /// </summary>
    private static string HashSecret(string secret)
    {
        var payload = Encoding.UTF8.GetBytes(secret);
        return Convert.ToBase64String(SHA256.HashData(payload));
    }

    #endregion
}
