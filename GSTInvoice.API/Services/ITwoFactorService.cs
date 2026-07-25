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
using GSTInvoice.API.Models;
using GSTInvoice.Shared.DTOs.Auth;

namespace GSTInvoice.API.Services;

/// <summary>
/// Service for managing Time-based One-Time Password (TOTP) 
/// authenticator app integration and two-factor authentication.
/// </summary>
public interface ITwoFactorService
{
    /// <summary>
    /// Generates a new TOTP secret for the user and returns setup information.
    /// </summary>
    Task<SetupTotpResponseDto> GenerateTotpSecretAsync(ApplicationUser user, string password);

    /// <summary>
    /// Verifies a TOTP code from an authenticator app to confirm setup.
    /// </summary>
    Task<bool> VerifyTotpSetupAsync(ApplicationUser user, string code);

    /// <summary>
    /// Validates a TOTP code during login.
    /// </summary>
    Task<bool> ValidateTotpCodeAsync(ApplicationUser user, string code);

    /// <summary>
    /// Disables TOTP for the user and clears the secret.
    /// </summary>
    Task DisableTotpAsync(ApplicationUser user);

    /// <summary>
    /// Checks if the user has TOTP enabled.
    /// </summary>
    Task<bool> IsTotpEnabledAsync(ApplicationUser user);
}
