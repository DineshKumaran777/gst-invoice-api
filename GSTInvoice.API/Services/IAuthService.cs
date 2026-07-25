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
using GSTInvoice.Shared.DTOs.Auth;

namespace GSTInvoice.API.Services;

public interface IAuthService
{
    Task<TokenResponseDto?> RegisterAsync(RegisterRequestDto request, CancellationToken cancellationToken = default);

    Task<TokenResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default);

    Task<RequestLoginOtpResponseDto> RequestLoginOtpAsync(RequestLoginOtpRequestDto request, CancellationToken cancellationToken = default);

    Task<TokenResponseDto> VerifyLoginOtpAsync(VerifyLoginOtpRequestDto request, CancellationToken cancellationToken = default);

    Task<RequestPasswordResetResponseDto> RequestPasswordResetAsync(RequestPasswordResetRequestDto request, CancellationToken cancellationToken = default);

    Task ResetPasswordAsync(ResetPasswordRequestDto request, CancellationToken cancellationToken = default);

    Task<TokenResponseDto> RefreshTokenAsync(RefreshTokenRequestDto request, CancellationToken cancellationToken = default);

    Task<RequestLoginOtpResponseDto> ResendLoginOtpAsync(ResendOtpRequestDto request, CancellationToken cancellationToken = default);

    Task<SetupTotpResponseDto> SetupTotpAsync(SetupTotpRequestDto request, CancellationToken cancellationToken = default);

    Task VerifyTotpSetupAsync(VerifyTotpSetupRequestDto request, CancellationToken cancellationToken = default);

    Task DisableTotpAsync(CancellationToken cancellationToken = default);
}

