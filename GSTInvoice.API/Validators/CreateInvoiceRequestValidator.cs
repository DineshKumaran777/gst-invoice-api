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
using GSTInvoice.Shared.DTOs.Invoice;

namespace GSTInvoice.API.Validators;

public class CreateInvoiceRequestValidator : AbstractValidator<CreateInvoiceRequestDto>
{
    public CreateInvoiceRequestValidator()
    {
        RuleFor(request => request.ClientId).NotEmpty();
        RuleFor(request => request.InvoiceNumber).NotEmpty().MaximumLength(50);
        RuleFor(request => request.PlaceOfSupply).NotEmpty().MaximumLength(100);
        RuleFor(request => request.DueDate)
            .GreaterThanOrEqualTo(request => request.InvoiceDate)
            .WithMessage("Due date cannot be before invoice date.");

        RuleForEach(request => request.Items)
            .SetValidator(new CreateInvoiceItemRequestValidator());
    }
}

public class CreateInvoiceItemRequestValidator : AbstractValidator<CreateInvoiceItemRequestDto>
{
    public CreateInvoiceItemRequestValidator()
    {
        RuleFor(request => request.Description).NotEmpty().MaximumLength(500);
        RuleFor(request => request.HSNCode).NotEmpty().MaximumLength(20);
        RuleFor(request => request.Quantity).GreaterThan(0);
        RuleFor(request => request.UnitPrice).GreaterThanOrEqualTo(0);
        RuleFor(request => request.DiscountPercentage).InclusiveBetween(0, 100);
    }
}

