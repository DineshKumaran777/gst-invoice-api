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
using GSTInvoice.API.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace GSTInvoice.API.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Tenant> Tenants => Set<Tenant>();

    public DbSet<Client> Clients => Set<Client>();

    public DbSet<Product> Products => Set<Product>();

    public DbSet<Invoice> Invoices => Set<Invoice>();

    public DbSet<InvoiceItem> InvoiceItems => Set<InvoiceItem>();

    public DbSet<Payment> Payments => Set<Payment>();

    public DbSet<Notification> Notifications => Set<Notification>();

    public DbSet<EmailLog> EmailLogs => Set<EmailLog>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public DbSet<SubscriptionPlan> SubscriptionPlans => Set<SubscriptionPlan>();

    public DbSet<OtpRecord> OtpRecords => Set<OtpRecord>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>()
            .HasOne(user => user.Tenant)
            .WithMany(tenant => tenant.Users)
            .HasForeignKey(user => user.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Client>()
            .HasOne(client => client.Tenant)
            .WithMany(tenant => tenant.Clients)
            .HasForeignKey(client => client.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Product>()
            .HasOne(product => product.Tenant)
            .WithMany(tenant => tenant.Products)
            .HasForeignKey(product => product.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Invoice>()
            .HasOne(invoice => invoice.Tenant)
            .WithMany(tenant => tenant.Invoices)
            .HasForeignKey(invoice => invoice.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Invoice>()
            .HasOne(invoice => invoice.Client)
            .WithMany(client => client.Invoices)
            .HasForeignKey(invoice => invoice.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Invoice>()
            .HasOne(invoice => invoice.CreatedByUser)
            .WithMany(user => user.InvoicesCreated)
            .HasForeignKey(invoice => invoice.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<InvoiceItem>()
            .HasOne(item => item.Invoice)
            .WithMany(invoice => invoice.Items)
            .HasForeignKey(item => item.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<InvoiceItem>()
            .HasOne(item => item.Product)
            .WithMany()
            .HasForeignKey(item => item.ProductId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<Payment>()
            .HasOne(payment => payment.Tenant)
            .WithMany()
            .HasForeignKey(payment => payment.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Payment>()
            .HasOne(payment => payment.Invoice)
            .WithMany(invoice => invoice.Payments)
            .HasForeignKey(payment => payment.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Notification>()
            .HasOne(notification => notification.Tenant)
            .WithMany()
            .HasForeignKey(notification => notification.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Notification>()
            .HasOne(notification => notification.User)
            .WithMany(user => user.Notifications)
            .HasForeignKey(notification => notification.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<EmailLog>()
            .HasOne(emailLog => emailLog.Invoice)
            .WithMany()
            .HasForeignKey(emailLog => emailLog.InvoiceId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<Invoice>().HasIndex(invoice => new { invoice.TenantId, invoice.InvoiceDate });
        builder.Entity<Invoice>().HasIndex(invoice => new { invoice.TenantId, invoice.Status });
        builder.Entity<Invoice>().HasIndex(invoice => new { invoice.TenantId, invoice.ClientId });
        builder.Entity<Invoice>().HasIndex(invoice => new { invoice.TenantId, invoice.InvoiceNumber }).IsUnique();

        builder.Entity<Client>().HasIndex(client => new { client.TenantId, client.Name });
        builder.Entity<Product>().HasIndex(product => new { product.TenantId, product.Name });
        builder.Entity<Payment>().HasIndex(payment => new { payment.TenantId, payment.DateUtc });
        builder.Entity<Notification>().HasIndex(notification => new { notification.TenantId, notification.UserId, notification.CreatedAtUtc });
        builder.Entity<AuditLog>().HasIndex(log => new { log.TenantId, log.TimestampUtc });
        builder.Entity<SubscriptionPlan>().HasIndex(plan => plan.Name).IsUnique();

        builder.Entity<Invoice>().Property(invoice => invoice.Subtotal).HasColumnType("decimal(18,2)");
        builder.Entity<Invoice>().Property(invoice => invoice.Discount).HasColumnType("decimal(18,2)");
        builder.Entity<Invoice>().Property(invoice => invoice.TaxableAmount).HasColumnType("decimal(18,2)");
        builder.Entity<Invoice>().Property(invoice => invoice.TotalCGST).HasColumnType("decimal(18,2)");
        builder.Entity<Invoice>().Property(invoice => invoice.TotalSGST).HasColumnType("decimal(18,2)");
        builder.Entity<Invoice>().Property(invoice => invoice.TotalIGST).HasColumnType("decimal(18,2)");
        builder.Entity<Invoice>().Property(invoice => invoice.GrandTotal).HasColumnType("decimal(18,2)");
        builder.Entity<Invoice>().Property(invoice => invoice.RoundOff).HasColumnType("decimal(18,2)");

        builder.Entity<InvoiceItem>().Property(item => item.Quantity).HasColumnType("decimal(18,2)");
        builder.Entity<InvoiceItem>().Property(item => item.UnitPrice).HasColumnType("decimal(18,2)");
        builder.Entity<InvoiceItem>().Property(item => item.Discount).HasColumnType("decimal(18,2)");
        builder.Entity<InvoiceItem>().Property(item => item.CGSTAmount).HasColumnType("decimal(18,2)");
        builder.Entity<InvoiceItem>().Property(item => item.SGSTAmount).HasColumnType("decimal(18,2)");
        builder.Entity<InvoiceItem>().Property(item => item.IGSTAmount).HasColumnType("decimal(18,2)");
        builder.Entity<InvoiceItem>().Property(item => item.TotalAmount).HasColumnType("decimal(18,2)");

        builder.Entity<Product>().Property(product => product.UnitPrice).HasColumnType("decimal(18,2)");
        builder.Entity<Payment>().Property(payment => payment.Amount).HasColumnType("decimal(18,2)");
        builder.Entity<SubscriptionPlan>().Property(plan => plan.PriceInrPerMonth).HasColumnType("decimal(18,2)");
        builder.Entity<Tenant>().Property(tenant => tenant.EstimatedSubscriptionChargeInr).HasColumnType("decimal(18,2)");
    }
}

