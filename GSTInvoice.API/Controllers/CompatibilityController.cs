using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using GSTInvoice.API.Options;
using GSTInvoice.API.Services;

namespace GSTInvoice.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
public class CompatibilityController : ControllerBase
{
    private readonly IJwtSigningKeyProvider _jwtKeyProvider;
    private readonly JwtOptions _jwtOptions;

    public CompatibilityController(IJwtSigningKeyProvider jwtKeyProvider, Microsoft.Extensions.Options.IOptions<JwtOptions> jwtOptions)
    {
        _jwtKeyProvider = jwtKeyProvider;
        _jwtOptions = jwtOptions.Value;
    }

    [AllowAnonymous]
    [HttpGet("api/v{version:apiVersion}/auth/session")]
    public IActionResult GetSession()
    {
        var authHeader = Request.Headers.Authorization.FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            var token = authHeader["Bearer ".Length..].Trim();
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var validationParams = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = _jwtOptions.Issuer,
                    ValidAudience = _jwtOptions.Audience,
                    IssuerSigningKey = _jwtKeyProvider.ValidationKey,
                    ClockSkew = TimeSpan.FromMinutes(2),
                };

                var principal = handler.ValidateToken(token, validationParams, out var validatedToken);
                var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
                var email = principal.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty;
                var fullName = principal.FindFirst("full_name")?.Value ?? string.Empty;
                var roleClaim = principal.FindFirst("user_role")?.Value ?? principal.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
                var tenantId = principal.FindFirst("tenant_id")?.Value ?? string.Empty;

                return Ok(new
                {
                    authenticated = true,
                    csrfToken = string.Empty,
                    user = new
                    {
                        userId,
                        tenantId,
                        fullName,
                        email,
                        role = roleClaim,
                    },
                    shell = new
                    {
                        unreadNotificationCount = 0,
                        subscriptionPlanName = "Workspace",
                    },
                });
            }
            catch (SecurityTokenExpiredException)
            {
                return Ok(new { authenticated = false, csrfToken = string.Empty });
            }
            catch (SecurityTokenException)
            {
                return Ok(new { authenticated = false, csrfToken = string.Empty });
            }
        }

        return Ok(new { authenticated = false, csrfToken = string.Empty });
    }

    [AllowAnonymous]
    [HttpPost("api/v{version:apiVersion}/auth/logout")]
    public IActionResult Logout() => Ok(new { message = "Logged out." });

    [AllowAnonymous]
    [HttpGet("api/v{version:apiVersion}/modules/{moduleKey}")]
    public IActionResult GetModule(string moduleKey)
    {
        var moduleName = moduleKey switch
        {
            "sales" => "Sales Command Center",
            "purchases" => "Purchases Desk",
            "inventory" => "Inventory Board",
            "suppliers" => "Supplier Operations",
            "expenses" => "Expense Control",
            "reports" => "Reporting Hub",
            "gst" => "GST Compliance Desk",
            "user-management" => "Access Governance",
            "admin" => "Admin Control Room",
            _ => "Operations Workspace",
        };

        return Ok(new
        {
            moduleName,
            headline = "Live module feed is currently unavailable.",
            description = "The module workspace is served from the API compatibility layer.",
            statusLabel = "Available",
            statusClass = "status-ok",
            stats = new[]
            {
                new { label = "Module", value = moduleName },
                new { label = "Availability", value = "Online" },
                new { label = "Data Source", value = "API compatibility" },
            },
            actions = new[]
            {
                new { label = "Open module route", url = "/modules", buttonStyleClass = "primary", isEnabled = true, hint = "Continue in module workspace." },
            },
            capabilities = new[]
            {
                "Route navigation remains available",
                "Core workflows continue to function",
            },
        });
    }
}
