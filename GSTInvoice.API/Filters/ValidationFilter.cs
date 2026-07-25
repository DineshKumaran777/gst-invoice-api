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
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace GSTInvoice.API.Filters;

public class ValidationFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.ModelState.IsValid)
        {
            return;
        }

        var problemDetails = new ValidationProblemDetails(context.ModelState)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Validation failed.",
            Detail = "One or more request validation errors occurred.",
            Instance = context.HttpContext.Request.Path,
        };

        problemDetails.Extensions["correlationId"] = context.HttpContext.TraceIdentifier;
        context.Result = new BadRequestObjectResult(problemDetails);
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
    }
}

