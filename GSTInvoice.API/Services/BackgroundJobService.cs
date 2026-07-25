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
using Hangfire;
using Microsoft.EntityFrameworkCore;
using System.IO.Compression;
using System.Text.Json;

namespace GSTInvoice.API.Services;

public class BackgroundJobService(
    IRecurringJobManager recurringJobManager,
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<BackgroundJobService> logger)
    : IBackgroundJobService
{
    public void ConfigureRecurringJobs()
    {
        recurringJobManager.AddOrUpdate(
            "mark-overdue-invoices",
            () => MarkOverdueInvoicesAsync(),
            Cron.Daily);

        recurringJobManager.AddOrUpdate(
            "send-payment-reminders",
            () => SendPaymentRemindersAsync(),
            Cron.Daily);

        recurringJobManager.AddOrUpdate(
            "weekly-gst-summary",
            () => SendWeeklyGstSummaryAsync(),
            Cron.Weekly);

        recurringJobManager.AddOrUpdate(
            "daily-platform-backup",
            () => RunDailyBackupAsync(),
            Cron.Daily(2));
    }

    public async Task MarkOverdueInvoicesAsync()
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Data.AppDbContext>();

        var now = DateTime.UtcNow.Date;
        var candidates = await dbContext.Invoices
            .Where(invoice => invoice.DueDate < now
                && invoice.Status != GSTInvoice.Shared.Enums.InvoiceStatus.Paid
                && invoice.Status != GSTInvoice.Shared.Enums.InvoiceStatus.Cancelled)
            .ToListAsync();

        foreach (var invoice in candidates)
        {
            invoice.Status = GSTInvoice.Shared.Enums.InvoiceStatus.Overdue;
            invoice.UpdatedAtUtc = DateTime.UtcNow;
        }

        if (candidates.Count > 0)
        {
            await dbContext.SaveChangesAsync();
        }

        logger.LogInformation("Marked {Count} overdue invoices.", candidates.Count);
    }

    public Task SendPaymentRemindersAsync()
    {
        logger.LogInformation("Scheduled payment reminder job executed.");
        return Task.CompletedTask;
    }

    public Task SendWeeklyGstSummaryAsync()
    {
        logger.LogInformation("Weekly GST summary job executed.");
        return Task.CompletedTask;
    }

    public async Task RunDailyBackupAsync()
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Data.AppDbContext>();

        var payload = new BackupPayload
        {
            GeneratedAtUtc = DateTime.UtcNow,
            Tenants = await dbContext.Tenants.AsNoTracking().ToListAsync(),
            Users = await dbContext.Users.AsNoTracking().Select(user => new
            {
                user.Id,
                user.TenantId,
                user.Email,
                user.FullName,
                user.Role,
                user.IsActive,
                user.TwoFactorEnabled,
            }).ToListAsync(),
            Clients = await dbContext.Clients.AsNoTracking().ToListAsync(),
            Products = await dbContext.Products.AsNoTracking().ToListAsync(),
            Invoices = await dbContext.Invoices
                .AsNoTracking()
                .Include(invoice => invoice.Items)
                .Include(invoice => invoice.Payments)
                .ToListAsync(),
            Payments = await dbContext.Payments.AsNoTracking().ToListAsync(),
            Notifications = await dbContext.Notifications.AsNoTracking().ToListAsync(),
        };

        var backupDirectory = configuration["Backup:Directory"];
        if (string.IsNullOrWhiteSpace(backupDirectory))
        {
            backupDirectory = Path.Combine(AppContext.BaseDirectory, "backups");
        }
        else if (!Path.IsPathRooted(backupDirectory))
        {
            backupDirectory = Path.Combine(AppContext.BaseDirectory, backupDirectory);
        }

        Directory.CreateDirectory(backupDirectory);

        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
        var jsonPath = Path.Combine(backupDirectory, $"gst-backup-{timestamp}.json");
        var zipPath = Path.Combine(backupDirectory, $"gst-backup-{timestamp}.zip");

        await System.IO.File.WriteAllTextAsync(
            jsonPath,
            JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                WriteIndented = true,
            }));

        using (var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
        {
            archive.CreateEntryFromFile(jsonPath, Path.GetFileName(jsonPath), CompressionLevel.SmallestSize);
        }

        System.IO.File.Delete(jsonPath);
        logger.LogInformation("Daily backup created at {ZipPath}", zipPath);
    }

    private sealed class BackupPayload
    {
        public DateTime GeneratedAtUtc { get; set; }

        public object? Users { get; set; }

        public object? Tenants { get; set; }

        public object? Clients { get; set; }

        public object? Products { get; set; }

        public object? Invoices { get; set; }

        public object? Payments { get; set; }

        public object? Notifications { get; set; }
    }
}

