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
using GSTInvoice.Shared.DTOs.Client;

namespace GSTInvoice.API.Validators;

public class UpsertClientRequestValidator : AbstractValidator<UpsertClientRequestDto>
{
    public UpsertClientRequestValidator()
    {
        RuleFor(request => request.Name).NotEmpty().MaximumLength(200);
        RuleFor(request => request.Email)
            .MaximumLength(256)
            .EmailAddress()
            .When(request => !string.IsNullOrWhiteSpace(request.Email));
        RuleFor(request => request.Phone)
            .Matches("^[6-9][0-9]{9}$")
            .When(request => !string.IsNullOrWhiteSpace(request.Phone));
        RuleFor(request => request.AlternatePhone)
            .Matches("^[6-9][0-9]{9}$")
            .When(request => !string.IsNullOrWhiteSpace(request.AlternatePhone));
        RuleFor(request => request.AddressLine1).NotEmpty().MaximumLength(200);
        RuleFor(request => request.City).NotEmpty().MaximumLength(100);
        RuleFor(request => request.State).NotEmpty().MaximumLength(100);
        RuleFor(request => request.Pincode).NotEmpty().Matches("^[1-9][0-9]{5}$");
        RuleFor(request => request.Country).NotEmpty().MaximumLength(100);
        RuleFor(request => request.GSTIN)
            .Matches("^[0-9]{2}[A-Z]{5}[0-9]{4}[A-Z]{1}[1-9A-Z]{1}Z[0-9A-Z]{1}$")
            .When(request => !string.IsNullOrWhiteSpace(request.GSTIN));
        RuleFor(request => request.PAN)
            .Matches("^[A-Z]{5}[0-9]{4}[A-Z]{1}$")
            .When(request => !string.IsNullOrWhiteSpace(request.PAN));
    }
}

