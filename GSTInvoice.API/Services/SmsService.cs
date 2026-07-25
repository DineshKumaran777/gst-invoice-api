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
using GSTInvoice.API.Options;
using Microsoft.Extensions.Options;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace GSTInvoice.API.Services;

public class SmsService(IOptions<TwilioOptions> options) : ISmsService
{
    private readonly TwilioOptions twilioOptions = options.Value;

    public async Task SendSmsAsync(string to, string message, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(twilioOptions.AccountSid) || string.IsNullOrWhiteSpace(twilioOptions.AuthToken))
        {
            return;
        }

        TwilioClient.Init(twilioOptions.AccountSid, twilioOptions.AuthToken);

        await MessageResource.CreateAsync(
            to: new PhoneNumber(to),
            from: new PhoneNumber(twilioOptions.SmsFromNumber),
            body: message);
    }
}

