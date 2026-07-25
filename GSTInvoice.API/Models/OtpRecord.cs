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
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GSTInvoice.API.Models;

/// <summary>
/// Represents a persisted one-time password record for OTP/TOTP verification.
/// Used for login OTP, password reset OTP, and TOTP setup verification.
/// </summary>
public class OtpRecord
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// The user who requested the OTP.
    /// </summary>
    [Required]
    [MaxLength(450)]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// The tenant this OTP belongs to.
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Type of OTP: Login, PasswordReset, TotpSetup, TwoFactor.
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string OtpType { get; set; } = string.Empty;

    /// <summary>
    /// SHA-256 hash of the OTP code, salted with user-specific data.
    /// </summary>
    [Required]
    [MaxLength(128)]
    public string OtpHash { get; set; } = string.Empty;

    /// <summary>
    /// The delivery channel: Email, Sms, or Authenticator.
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string DeliveryChannel { get; set; } = "Email";

    /// <summary>
    /// Masked destination for display (e.g., j***@example.com).
    /// </summary>
    [MaxLength(256)]
    public string? DestinationMasked { get; set; }

    /// <summary>
    /// UTC time when this OTP expires.
    /// </summary>
    public DateTime ExpiresAtUtc { get; set; }

    /// <summary>
    /// Number of failed verification attempts.
    /// </summary>
    public int AttemptCount { get; set; }

    /// <summary>
    /// Maximum allowed attempts before OTP is invalidated.
    /// </summary>
    public int MaxAttempts { get; set; } = 5;

    /// <summary>
    /// Number of times the OTP has been resent.
    /// </summary>
    public int ResendCount { get; set; }

    /// <summary>
    /// Maximum allowed resends before the user must request a new OTP.
    /// </summary>
    public int MaxResends { get; set; } = 3;

    /// <summary>
    /// Whether this OTP has been successfully used/consumed.
    /// Prevents replay attacks.
    /// </summary>
    public bool IsUsed { get; set; }

    /// <summary>
    /// UTC timestamp when this record was created.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// UTC timestamp when this OTP was successfully verified (if applicable).
    /// </summary>
    public DateTime? VerifiedAtUtc { get; set; }

    /// <summary>
    /// Navigation property to the user.
    /// </summary>
    [ForeignKey(nameof(UserId))]
    public ApplicationUser? User { get; set; }
}
