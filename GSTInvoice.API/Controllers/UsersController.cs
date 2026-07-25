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
using GSTInvoice.Shared.Pagination;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GSTInvoice.API.Controllers;

[ApiController]
[Authorize(Policy = "CompanyAdminOnly")]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/users")]
public class UsersController(IUserService userService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<UserSummaryDto>>> GetUsers([FromQuery] PaginationRequest request, CancellationToken cancellationToken)
    {
        var result = await userService.GetUsersAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("invite")]
    public async Task<ActionResult<UserSummaryDto>> Invite(InviteUserRequestDto request, CancellationToken cancellationToken)
    {
        var result = await userService.InviteAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{userId}")]
    public async Task<IActionResult> Deactivate(string userId, CancellationToken cancellationToken)
    {
        var success = await userService.DeactivateAsync(userId, cancellationToken);
        return success ? NoContent() : NotFound();
    }
}

