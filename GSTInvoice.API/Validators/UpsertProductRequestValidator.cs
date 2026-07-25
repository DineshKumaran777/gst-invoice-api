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
using FluentValidation;
using GSTInvoice.Shared.DTOs.Product;

namespace GSTInvoice.API.Validators;

public class UpsertProductRequestValidator : AbstractValidator<UpsertProductRequestDto>
{
    public UpsertProductRequestValidator()
    {
        RuleFor(request => request.Name).NotEmpty().MaximumLength(200);
        RuleFor(request => request.HSNCode).NotEmpty().MaximumLength(20);
        RuleFor(request => request.DefaultUnitPrice).GreaterThanOrEqualTo(0);
        RuleFor(request => request.UnitOfMeasure).NotEmpty().MaximumLength(20);
    }
}

