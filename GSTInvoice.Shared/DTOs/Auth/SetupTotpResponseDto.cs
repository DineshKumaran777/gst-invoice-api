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
namespace GSTInvoice.Shared.DTOs.Auth;

/// <summary>
/// Response containing TOTP setup information for authenticator app enrollment.
/// </summary>
public class SetupTotpResponseDto
{
    /// <summary>
    /// The shared secret key in Base32 format for manual entry.
    /// </summary>
    public string SharedKey { get; set; } = string.Empty;

    /// <summary>
    /// The URI for QR code generation (otpauth:// protocol).
    /// </summary>
    public string AuthenticatorUri { get; set; } = string.Empty;

    /// <summary>
    /// Whether TOTP is already enabled for this user.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Whether the user has a TOTP secret configured (may not be verified yet).
    /// </summary>
    public bool HasSecret { get; set; }
}
