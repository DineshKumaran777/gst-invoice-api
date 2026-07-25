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
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace GSTInvoice.API.Middleware;

public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            var details = BuildProblemDetails(context, exception);

            if (details.Status >= (int)HttpStatusCode.InternalServerError)
            {
                logger.LogError(exception, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);
            }
            else
            {
                logger.LogWarning(
                    "Handled exception for {Method} {Path} with status {StatusCode}: {Detail}",
                    context.Request.Method,
                    context.Request.Path,
                    details.Status,
                    details.Detail);
            }

            context.Response.StatusCode = details.Status ?? StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(details, SerializerOptions));
        }
    }

    private static ProblemDetails BuildProblemDetails(HttpContext context, Exception exception)
    {
        var (statusCode, title, detail) = exception switch
        {
            UnauthorizedAccessException => ((int)HttpStatusCode.Unauthorized, "Unauthorized", exception.Message),
            KeyNotFoundException => ((int)HttpStatusCode.NotFound, "Not Found", exception.Message),
            ArgumentException => ((int)HttpStatusCode.BadRequest, "Bad Request", exception.Message),
            InvalidOperationException => ((int)HttpStatusCode.BadRequest, "Invalid Operation", exception.Message),
            _ => ((int)HttpStatusCode.InternalServerError, "Internal Server Error", "An unexpected error occurred. Please retry or contact support."),
        };

        var details = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path,
        };

        details.Extensions["correlationId"] = context.TraceIdentifier;
        return details;
    }
}

