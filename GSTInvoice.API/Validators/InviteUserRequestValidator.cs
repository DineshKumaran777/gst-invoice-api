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
using GSTInvoice.Shared.DTOs.Auth;
using GSTInvoice.Shared.Enums;

namespace GSTInvoice.API.Validators;

public class InviteUserRequestValidator : AbstractValidator<InviteUserRequestDto>
{
    public InviteUserRequestValidator()
    {
        RuleFor(request => request.FullName).NotEmpty().MaximumLength(120);
        RuleFor(request => request.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(request => request.Role)
            .NotEqual(UserRole.SuperAdmin)
            .WithMessage("SuperAdmin role cannot be assigned for tenant users.");

        RuleFor(request => request.TemporaryPassword)
            .NotEmpty()
            .MinimumLength(8)
            .MaximumLength(64)
            .Matches("[A-Z]").WithMessage("Temporary password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("Temporary password must contain at least one lowercase letter.")
            .Matches("[0-9]").WithMessage("Temporary password must contain at least one digit.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Temporary password must contain at least one special character.");
    }
}
