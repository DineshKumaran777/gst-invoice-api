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
using GSTInvoice.API.Services;
using GSTInvoice.Shared.DTOs.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GSTInvoice.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth")]
public class AuthController(IAuthService authService) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<ActionResult<TokenResponseDto>> Register(RegisterRequestDto request, CancellationToken cancellationToken)
    {
        var result = await authService.RegisterAsync(request, cancellationToken);
        if (result is null)
            return Ok(new { message = "Email already exists." });
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [EnableRateLimiting("LoginLimiter")]
    public async Task<ActionResult<TokenResponseDto>> Login(LoginRequestDto request, CancellationToken cancellationToken)
    {
        var result = await authService.LoginAsync(request, cancellationToken);
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpPost("request-otp")]
    [EnableRateLimiting("LoginLimiter")]
    public async Task<ActionResult<RequestLoginOtpResponseDto>> RequestOtp(RequestLoginOtpRequestDto request, CancellationToken cancellationToken)
    {
        var result = await authService.RequestLoginOtpAsync(request, cancellationToken);
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpPost("verify-otp")]
    [EnableRateLimiting("LoginLimiter")]
    public async Task<ActionResult<TokenResponseDto>> VerifyOtp(VerifyLoginOtpRequestDto request, CancellationToken cancellationToken)
    {
        var result = await authService.VerifyLoginOtpAsync(request, cancellationToken);
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpPost("request-password-reset")]
    [EnableRateLimiting("LoginLimiter")]
    public async Task<ActionResult<RequestPasswordResetResponseDto>> RequestPasswordReset(RequestPasswordResetRequestDto request, CancellationToken cancellationToken)
    {
        var result = await authService.RequestPasswordResetAsync(request, cancellationToken);
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpPost("reset-password")]
    [EnableRateLimiting("LoginLimiter")]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequestDto request, CancellationToken cancellationToken)
    {
        await authService.ResetPasswordAsync(request, cancellationToken);
        return NoContent();
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<ActionResult<TokenResponseDto>> Refresh(RefreshTokenRequestDto request, CancellationToken cancellationToken)
    {
        var result = await authService.RefreshTokenAsync(request, cancellationToken);
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpPost("resend-otp")]
    [EnableRateLimiting("LoginLimiter")]
    public async Task<ActionResult<RequestLoginOtpResponseDto>> ResendOtp(ResendOtpRequestDto request, CancellationToken cancellationToken)
    {
        var result = await authService.ResendLoginOtpAsync(request, cancellationToken);
        return Ok(result);
    }

    [Authorize]
    [HttpPost("setup-totp")]
    public async Task<ActionResult<SetupTotpResponseDto>> SetupTotp(SetupTotpRequestDto request, CancellationToken cancellationToken)
    {
        var result = await authService.SetupTotpAsync(request, cancellationToken);
        return Ok(result);
    }

    [Authorize]
    [HttpPost("verify-totp-setup")]
    public async Task<IActionResult> VerifyTotpSetup(VerifyTotpSetupRequestDto request, CancellationToken cancellationToken)
    {
        await authService.VerifyTotpSetupAsync(request, cancellationToken);
        return Ok(new { message = "Two-factor authentication has been enabled successfully." });
    }

    [Authorize]
    [HttpPost("disable-totp")]
    public async Task<IActionResult> DisableTotp(CancellationToken cancellationToken)
    {
        await authService.DisableTotpAsync(cancellationToken);
        return Ok(new { message = "Two-factor authentication has been disabled." });
    }
}

