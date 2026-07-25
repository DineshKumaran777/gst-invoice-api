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
using GSTInvoice.API.Data;
using GSTInvoice.API.Models;
using GSTInvoice.Shared.DTOs.Invoice;
using GSTInvoice.Shared.Enums;
using GSTInvoice.Shared.Pagination;
using Microsoft.EntityFrameworkCore;

using GSTInvoice.Shared.DTOs.Client;
using GSTInvoice.Shared.DTOs.Tenant;

namespace GSTInvoice.API.Services;

public class InvoiceService(
    AppDbContext dbContext,
    ITenantContextAccessor tenantContextAccessor,
    INotificationService notificationService,
    IEmailService emailService,
    IWhatsAppService whatsAppService,
    IPdfService pdfService)
    : IInvoiceService
{
    public async Task<PagedResult<InvoiceDto>> GetPagedAsync(PaginationRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContextAccessor.GetTenantId();
        IQueryable<Invoice> query = dbContext.Invoices
            .AsNoTracking()
            .Where(invoice => invoice.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(invoice => invoice.InvoiceNumber.Contains(term));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var entities = await query
            .Include(invoice => invoice.Client)
            .Include(invoice => invoice.Items)
            .OrderByDescending(invoice => invoice.CreatedAtUtc)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var items = entities.Select(MapInvoiceDto).ToList();

        return new PagedResult<InvoiceDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
        };
    }

    public async Task<InvoiceDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContextAccessor.GetTenantId();
        var entity = await dbContext.Invoices
            .AsNoTracking()
            .Include(invoice => invoice.Client)
            .Include(invoice => invoice.Items)
            .Where(invoice => invoice.TenantId == tenantId && invoice.Id == id)
            .FirstOrDefaultAsync(cancellationToken);

        return entity is null ? null : MapInvoiceDto(entity);
    }

    public async Task<InvoiceDetailsDto?> GetDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContextAccessor.GetTenantId();

        var invoiceEntity = await dbContext.Invoices
            .AsNoTracking()
            .Include(invoice => invoice.Client)
            .Include(invoice => invoice.Items)
            .Where(invoice => invoice.TenantId == tenantId && invoice.Id == id)
            .FirstOrDefaultAsync(cancellationToken);

        if (invoiceEntity is null)
        {
            return null;
        }

        var tenant = await dbContext.Tenants
            .AsNoTracking()
            .FirstAsync(entity => entity.Id == tenantId, cancellationToken);

        var invoiceDto = MapInvoiceDto(invoiceEntity);

        var seller = new CompanySettingsDto
        {
            TenantId = tenant.Id,
            Name = tenant.Name,
            GSTIN = tenant.GSTIN,
            PAN = tenant.PAN,
            Address = tenant.Address,
            State = tenant.State,
            LogoUrl = tenant.LogoUrl,
            SubscriptionPlan = tenant.SubscriptionPlan,
            IsTrialActive = tenant.TrialStartsAtUtc <= DateTime.UtcNow && tenant.TrialEndsAtUtc >= DateTime.UtcNow,
            TrialEndsAtUtc = tenant.TrialEndsAtUtc,
            NextRenewalAtUtc = tenant.NextRenewalAtUtc,
            EstimatedSubscriptionChargeInr = tenant.EstimatedSubscriptionChargeInr,
            AppliedCouponCode = tenant.AppliedCouponCode,
        };

        var client = new ClientDto
        {
            Id = invoiceEntity.Client!.Id,
            TenantId = invoiceEntity.Client.TenantId,
            Name = invoiceEntity.Client.Name,
            Email = invoiceEntity.Client.Email,
            Phone = invoiceEntity.Client.Phone,
            AlternatePhone = invoiceEntity.Client.AlternatePhone,
            AddressLine1 = invoiceEntity.Client.AddressLine1,
            AddressLine2 = invoiceEntity.Client.AddressLine2,
            City = invoiceEntity.Client.City,
            State = invoiceEntity.Client.State,
            Pincode = invoiceEntity.Client.Pincode,
            Country = invoiceEntity.Client.Country,
            GSTIN = invoiceEntity.Client.GSTIN,
            PAN = invoiceEntity.Client.PAN,
            BusinessType = invoiceEntity.Client.BusinessType,
            ContactPersonName = invoiceEntity.Client.ContactPersonName,
            CreatedAtUtc = invoiceEntity.Client.CreatedAtUtc,
        };

        var isSameState = string.Equals(tenant.State, client.State, StringComparison.OrdinalIgnoreCase);

        return new InvoiceDetailsDto
        {
            Invoice = invoiceDto,
            Seller = seller,
            Client = client,
            IsSameState = isSameState,
            AmountInWords = AmountInWords(invoiceDto.GrandTotal),
            TemplateName = "Default",
            BrandHexColor = "#0a84ff",
            PdfUrl = $"/api/v1/invoices/{id}/pdf",
        };
    }

    public async Task<byte[]?> GeneratePdfAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContextAccessor.GetTenantId();

        var entity = await dbContext.Invoices
            .AsNoTracking()
            .Include(invoice => invoice.Client)
            .Include(invoice => invoice.Items)
            .Where(invoice => invoice.TenantId == tenantId && invoice.Id == id)
            .FirstOrDefaultAsync(cancellationToken);

        if (entity is null)
        {
            return null;
        }

        var tenant = await dbContext.Tenants
            .AsNoTracking()
            .FirstAsync(entity => entity.Id == tenantId, cancellationToken);

        var invoiceDto = MapInvoiceDto(entity);
        return pdfService.GenerateInvoicePdf(invoiceDto, tenant.Name, tenant.GSTIN, tenant.Address);
    }

    public async Task<InvoiceDto> CreateAsync(CreateInvoiceRequestDto request, CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContextAccessor.GetTenantId();
        var userId = tenantContextAccessor.GetUserId();

        var tenant = await dbContext.Tenants.FirstAsync(entity => entity.Id == tenantId, cancellationToken);
        var client = await dbContext.Clients.FirstAsync(entity => entity.TenantId == tenantId && entity.Id == request.ClientId, cancellationToken);

        var activePlanName = NormalizePlanName(tenant.SubscriptionPlan);
        var activePlan = await dbContext.SubscriptionPlans
            .AsNoTracking()
            .FirstOrDefaultAsync(entity => entity.IsActive && entity.Name == activePlanName, cancellationToken);

        if (activePlan is not null && activePlan.MaxInvoicesPerMonth > 0 && activePlan.MaxInvoicesPerMonth < int.MaxValue)
        {
            var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            var monthEnd = monthStart.AddMonths(1);

            var invoiceCountThisMonth = await dbContext.Invoices
                .AsNoTracking()
                .CountAsync(entity => entity.TenantId == tenantId
                    && entity.InvoiceDate >= monthStart
                    && entity.InvoiceDate < monthEnd,
                    cancellationToken);

            if (invoiceCountThisMonth >= activePlan.MaxInvoicesPerMonth)
            {
                throw new InvalidOperationException($"Invoice limit reached for the {activePlan.Name} plan. Upgrade your subscription to continue.");
            }
        }

        var isSameState = string.Equals(tenant.State, client.State, StringComparison.OrdinalIgnoreCase);

        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ClientId = request.ClientId,
            CreatedByUserId = userId,
            InvoiceNumber = request.InvoiceNumber.Trim().ToUpperInvariant(),
            InvoiceType = request.InvoiceType,
            InvoiceDate = request.InvoiceDate,
            DueDate = request.DueDate,
            PlaceOfSupply = request.PlaceOfSupply,
            PONumber = request.PONumber,
            ReferenceNumber = request.ReferenceNumber,
            Notes = request.Notes,
            Terms = request.Terms,
            Status = InvoiceStatus.Draft,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            Client = client,
        };

        foreach (var itemRequest in request.Items)
        {
            var lineAmount = itemRequest.Quantity * itemRequest.UnitPrice;
            var discountAmount = lineAmount * (itemRequest.DiscountPercentage / 100m);
            var taxableAmount = lineAmount - discountAmount;
            var gstRate = (decimal)itemRequest.GSTRate;
            var totalTaxAmount = taxableAmount * (gstRate / 100m);

            var cgst = isSameState ? totalTaxAmount / 2m : 0m;
            var sgst = isSameState ? totalTaxAmount / 2m : 0m;
            var igst = isSameState ? 0m : totalTaxAmount;

            invoice.Items.Add(new InvoiceItem
            {
                Id = Guid.NewGuid(),
                ProductId = itemRequest.ProductId,
                Description = itemRequest.Description,
                HSNCode = itemRequest.HSNCode,
                Quantity = itemRequest.Quantity,
                Unit = itemRequest.Unit,
                UnitPrice = itemRequest.UnitPrice,
                Discount = itemRequest.DiscountPercentage,
                GSTRate = itemRequest.GSTRate,
                CGSTAmount = cgst,
                SGSTAmount = sgst,
                IGSTAmount = igst,
                TotalAmount = taxableAmount + totalTaxAmount,
            });

            invoice.Subtotal += lineAmount;
            invoice.Discount += discountAmount;
            invoice.TaxableAmount += taxableAmount;
            invoice.TotalCGST += cgst;
            invoice.TotalSGST += sgst;
            invoice.TotalIGST += igst;
        }

        invoice.GrandTotal = invoice.TaxableAmount + invoice.TotalCGST + invoice.TotalSGST + invoice.TotalIGST;
        invoice.RoundOff = Math.Round(invoice.GrandTotal, 0, MidpointRounding.AwayFromZero) - invoice.GrandTotal;

        dbContext.Invoices.Add(invoice);
        await dbContext.SaveChangesAsync(cancellationToken);

        await notificationService.NotifyTenantAsync(
            tenantId,
            "Invoice Created",
            $"Invoice {invoice.InvoiceNumber} has been created.",
            NotificationType.System,
            cancellationToken);

        return MapInvoiceDto(invoice);
    }

    public async Task<InvoiceDto?> UpdateStatusAsync(Guid id, UpdateInvoiceStatusRequestDto request, CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContextAccessor.GetTenantId();
        var invoice = await dbContext.Invoices
            .Include(entity => entity.Client)
            .FirstOrDefaultAsync(entity => entity.TenantId == tenantId && entity.Id == id, cancellationToken);

        if (invoice is null)
        {
            return null;
        }

        invoice.Status = request.Status;
        invoice.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        if (request.Status == InvoiceStatus.Paid)
        {
            await notificationService.NotifyTenantAsync(
                tenantId,
                "Invoice Paid",
                $"Invoice {invoice.InvoiceNumber} was marked as paid.",
                NotificationType.InvoicePaid,
                cancellationToken);
        }

        return MapInvoiceDto(invoice);
    }

    public async Task<bool> SendEmailAsync(Guid id, SendInvoiceEmailRequestDto request, CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContextAccessor.GetTenantId();

        var invoice = await dbContext.Invoices
            .Include(entity => entity.Client)
            .Include(entity => entity.Items)
            .FirstOrDefaultAsync(entity => entity.TenantId == tenantId && entity.Id == id, cancellationToken);

        if (invoice is null || invoice.Client is null)
        {
            return false;
        }

        var tenant = await dbContext.Tenants.FirstAsync(entity => entity.Id == tenantId, cancellationToken);
        var toEmail = string.IsNullOrWhiteSpace(request.ToEmail)
            ? invoice.Client.Email
            : request.ToEmail.Trim();

        if (string.IsNullOrWhiteSpace(toEmail))
        {
            return false;
        }

        var invoiceDto = MapInvoiceDto(invoice);
        var pdfBytes = pdfService.GenerateInvoicePdf(invoiceDto, tenant.Name, tenant.GSTIN, tenant.Address);

        var subject = string.IsNullOrWhiteSpace(request.Subject)
            ? $"Invoice {invoice.InvoiceNumber} from {tenant.Name}"
            : request.Subject.Trim();

        var body = string.IsNullOrWhiteSpace(request.Message)
            ? $"<p>Dear Customer,</p><p>Please find attached invoice <strong>{invoice.InvoiceNumber}</strong>.</p><p>Regards,<br/>{tenant.Name}</p>"
            : request.Message;

        var sent = await emailService.SendInvoiceEmailAsync(
            tenantId,
            invoice.Id,
            toEmail,
            subject,
            body,
            pdfBytes,
            $"{invoice.InvoiceNumber}.pdf",
            request.Cc,
            request.Bcc,
            cancellationToken);

        invoice.EmailStatus = sent ? "Sent" : "Failed";
        invoice.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        if (sent)
        {
            await notificationService.NotifyTenantAsync(
                tenantId,
                "Invoice Email Sent",
                $"Invoice {invoice.InvoiceNumber} was emailed to {toEmail}.",
                NotificationType.System,
                cancellationToken);
        }

        return sent;
    }

    public async Task<bool> SendWhatsAppAsync(Guid id, SendInvoiceWhatsAppRequestDto request, CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContextAccessor.GetTenantId();

        var invoice = await dbContext.Invoices
            .Include(entity => entity.Client)
            .FirstOrDefaultAsync(entity => entity.TenantId == tenantId && entity.Id == id, cancellationToken);

        if (invoice is null || invoice.Client is null)
        {
            return false;
        }

        var toPhone = string.IsNullOrWhiteSpace(request.ToPhone)
            ? invoice.Client.Phone
            : request.ToPhone.Trim();

        if (string.IsNullOrWhiteSpace(toPhone))
        {
            return false;
        }

        var message = string.IsNullOrWhiteSpace(request.Message)
            ? $"Invoice {invoice.InvoiceNumber} amount {invoice.GrandTotal:N2} is available. Please clear before {invoice.DueDate:dd MMM yyyy}."
            : request.Message;

        try
        {
            await whatsAppService.SendMessageAsync(toPhone, message, cancellationToken);
            invoice.SmsStatus = "WhatsApp Sent";
            invoice.UpdatedAtUtc = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);

            await notificationService.NotifyTenantAsync(
                tenantId,
                "WhatsApp Notification Sent",
                $"Invoice {invoice.InvoiceNumber} WhatsApp reminder sent to {toPhone}.",
                NotificationType.System,
                cancellationToken);

            return true;
        }
        catch
        {
            invoice.SmsStatus = "WhatsApp Failed";
            invoice.UpdatedAtUtc = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
            return false;
        }
    }

    private static InvoiceDto MapInvoiceDto(Invoice entity)
    {
        return new InvoiceDto
        {
            Id = entity.Id,
            TenantId = entity.TenantId,
            ClientId = entity.ClientId,
            ClientName = entity.Client?.Name ?? string.Empty,
            ClientEmail = entity.Client?.Email,
            ClientPhone = entity.Client?.Phone,
            CreatedByUserId = entity.CreatedByUserId,
            InvoiceNumber = entity.InvoiceNumber,
            InvoiceType = entity.InvoiceType,
            InvoiceDate = entity.InvoiceDate,
            DueDate = entity.DueDate,
            PlaceOfSupply = entity.PlaceOfSupply,
            PONumber = entity.PONumber,
            ReferenceNumber = entity.ReferenceNumber,
            Subtotal = entity.Subtotal,
            Discount = entity.Discount,
            TaxableAmount = entity.TaxableAmount,
            TotalCGST = entity.TotalCGST,
            TotalSGST = entity.TotalSGST,
            TotalIGST = entity.TotalIGST,
            GrandTotal = entity.GrandTotal,
            RoundOff = entity.RoundOff,
            Status = entity.Status,
            Notes = entity.Notes,
            Terms = entity.Terms,
            EmailStatus = entity.EmailStatus,
            SmsStatus = entity.SmsStatus,
            CreatedAtUtc = entity.CreatedAtUtc,
            UpdatedAtUtc = entity.UpdatedAtUtc,
            Items = entity.Items
                .OrderBy(item => item.Id)
                .Select(item => new InvoiceItemDto
                {
                    Id = item.Id,
                    ProductId = item.ProductId,
                    Description = item.Description,
                    HSNCode = item.HSNCode,
                    Quantity = item.Quantity,
                    Unit = item.Unit,
                    UnitPrice = item.UnitPrice,
                    DiscountPercentage = item.Discount,
                    GSTRate = item.GSTRate,
                    CGSTAmount = item.CGSTAmount,
                    SGSTAmount = item.SGSTAmount,
                    IGSTAmount = item.IGSTAmount,
                    TotalAmount = item.TotalAmount,
                })
                .ToList(),
        };
    }

    private static string NormalizePlanName(string subscriptionPlan)
    {
        var openingIndex = subscriptionPlan.IndexOf('(');
        if (openingIndex <= 0)
        {
            return subscriptionPlan.Trim();
        }

        return subscriptionPlan[..openingIndex].Trim();
    }

    private static string AmountInWords(decimal value)
    {
        var integerPart = (long)Math.Floor(value);

        if (integerPart <= 0)
        {
            return "Zero Rupees Only";
        }

        return $"{NumberToWords(integerPart)} Rupees Only";
    }

    private static string NumberToWords(long value)
    {
        if (value < 20)
        {
            return Ones[value];
        }

        if (value < 100)
        {
            return Tens[value / 10] + (value % 10 > 0 ? " " + Ones[value % 10] : "");
        }

        if (value < 1000)
        {
            return Ones[value / 100] + " Hundred" + (value % 100 > 0 ? " " + NumberToWords(value % 100) : "");
        }

        if (value < 100000)
        {
            return NumberToWords(value / 1000) + " Thousand" + (value % 1000 > 0 ? " " + NumberToWords(value % 1000) : "");
        }

        if (value < 10000000)
        {
            return NumberToWords(value / 100000) + " Lakh" + (value % 100000 > 0 ? " " + NumberToWords(value % 100000) : "");
        }

        return NumberToWords(value / 10000000) + " Crore" + (value % 10000000 > 0 ? " " + NumberToWords(value % 10000000) : "");
    }

    private static readonly string[] Ones =
    [
        "", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten",
        "Eleven", "Twelve", "Thirteen", "Fourteen", "Fifteen", "Sixteen", "Seventeen", "Eighteen", "Nineteen",
    ];

    private static readonly string[] Tens =
    [
        "", "", "Twenty", "Thirty", "Forty", "Fifty", "Sixty", "Seventy", "Eighty", "Ninety",
    ];
}

