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
using System.Net.Mail;
using GSTInvoice.API.Data;
using GSTInvoice.API.Models;
using GSTInvoice.API.Options;
using Microsoft.Extensions.Options;

namespace GSTInvoice.API.Services;

public class EmailService(
    AppDbContext dbContext,
    IOptions<EmailOptions> options,
    ILogger<EmailService> logger)
    : IEmailService
{
    private readonly EmailOptions emailOptions = options.Value;

    public async Task<bool> SendInvoiceEmailAsync(
        Guid tenantId,
        Guid? invoiceId,
        string toEmail,
        string subject,
        string body,
        byte[]? attachment = null,
        string? attachmentName = null,
        string? cc = null,
        string? bcc = null,
        CancellationToken cancellationToken = default)
    {
        var emailLog = new EmailLog
        {
            TenantId = tenantId,
            InvoiceId = invoiceId,
            ToEmail = toEmail,
            Subject = subject,
            Status = "Sent",
            SentAtUtc = DateTime.UtcNow,
        };

        var isSuccess = true;

        try
        {
            using var message = new MailMessage
            {
                From = new MailAddress(emailOptions.FromEmail, emailOptions.FromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true,
            };

            message.To.Add(new MailAddress(toEmail));

            if (!string.IsNullOrWhiteSpace(cc))
            {
                message.CC.Add(new MailAddress(cc));
            }

            if (!string.IsNullOrWhiteSpace(bcc))
            {
                message.Bcc.Add(new MailAddress(bcc));
            }

            if (attachment is not null && attachment.Length > 0)
            {
                var stream = new MemoryStream(attachment);
                var mailAttachment = new Attachment(stream, attachmentName ?? "invoice.pdf", "application/pdf");
                message.Attachments.Add(mailAttachment);
            }

            using var smtpClient = new SmtpClient(emailOptions.SmtpHost, emailOptions.SmtpPort)
            {
                EnableSsl = emailOptions.UseSsl,
                Credentials = new NetworkCredential(emailOptions.Username, emailOptions.Password),
            };

            await smtpClient.SendMailAsync(message, cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Email send failed for {ToEmail}", MaskEmail(toEmail));
            emailLog.Status = "Failed";
            emailLog.ErrorMessage = exception.Message;
            isSuccess = false;
        }

        dbContext.EmailLogs.Add(emailLog);
        await dbContext.SaveChangesAsync(cancellationToken);

        return isSuccess;
    }

    private static string MaskEmail(string email)
    {
        var atIndex = email.IndexOf('@');
        if (atIndex <= 1)
        {
            return "***";
        }

        return $"{email[0]}***{email[(atIndex - 1)..]}";
    }
}

