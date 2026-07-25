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
using GSTInvoice.Shared.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GSTInvoice.API.Data.Seeders;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider services, bool isSqlite = false)
    {
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        if (isSqlite)
        {
            await dbContext.Database.EnsureCreatedAsync();
        }
        else
        {
            await dbContext.Database.MigrateAsync();
        }

        foreach (var roleName in Enum.GetNames<UserRole>())
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }

        await SeedSubscriptionPlansAsync(dbContext);

        var superTenant = await dbContext.Tenants.FirstOrDefaultAsync(tenant => tenant.Name == "Global Super Admin Tenant");
        if (superTenant is null)
        {
            superTenant = new Tenant
            {
                Name = "Global Super Admin Tenant",
                GSTIN = "29AAAAA0000A1Z5",
                PAN = "AAAAA0000A",
                Address = "Global HQ",
                State = "Karnataka",
                SubscriptionPlan = "Enterprise",
                OtpLoginRequired = true,
                SessionTimeoutMinutes = 60,
                NextRenewalAtUtc = DateTime.UtcNow.Date.AddMonths(1),
                EstimatedSubscriptionChargeInr = 5000,
                BusinessType = BusinessType.Company,
                IsActive = true,
            };
            dbContext.Tenants.Add(superTenant);
            await dbContext.SaveChangesAsync();
        }

        var demoTenant = await dbContext.Tenants.FirstOrDefaultAsync(tenant => tenant.Name == "Demo GST Ventures");
        if (demoTenant is null)
        {
            demoTenant = new Tenant
            {
                Name = "Demo GST Ventures",
                GSTIN = "29ABCDE1234F1Z5",
                PAN = "ABCDE1234F",
                Address = "123 Demo Street, Bengaluru",
                State = "Karnataka",
                SubscriptionPlan = "Growth",
                OtpLoginRequired = false,
                SessionTimeoutMinutes = 30,
                TrialStartsAtUtc = DateTime.UtcNow.Date,
                TrialEndsAtUtc = DateTime.UtcNow.Date.AddDays(14),
                NextRenewalAtUtc = DateTime.UtcNow.Date.AddMonths(1),
                EstimatedSubscriptionChargeInr = 999,
                BusinessType = BusinessType.Company,
                IsActive = true,
            };
            dbContext.Tenants.Add(demoTenant);
            await dbContext.SaveChangesAsync();
        }

        ApplyTenantDefaults(superTenant);
        ApplyTenantDefaults(demoTenant);
        await dbContext.SaveChangesAsync();

        await EnsureUserAsync(userManager, "superadmin@gstinvoice.com", "Admin@123", "Super Admin", superTenant.Id, UserRole.SuperAdmin);
        await EnsureUserAsync(userManager, "demo@test.com", "Demo@123", "Demo Company Admin", demoTenant.Id, UserRole.CompanyAdmin);

        // Additional users for the demo tenant
        await EnsureUserAsync(userManager, "accountant@demo.com", "Demo@123", "Priya Accountant", demoTenant.Id, UserRole.Staff);
        await EnsureUserAsync(userManager, "auditor@demo.com", "Demo@123", "Rahul Auditor", demoTenant.Id, UserRole.Viewer);
        await EnsureUserAsync(userManager, "manager@demo.com", "Demo@123", "Anita Manager", demoTenant.Id, UserRole.CompanyAdmin);
        await EnsureUserAsync(userManager, "staff2@demo.com", "Demo@123", "Vikram Staff", demoTenant.Id, UserRole.Staff);
        await EnsureUserAsync(userManager, "viewer2@demo.com", "Demo@123", "Sneha Viewer", demoTenant.Id, UserRole.Viewer);

        await SeedDemoBusinessDataAsync(dbContext, demoTenant.Id, "demo@test.com");
    }

    private static async Task EnsureUserAsync(
        UserManager<ApplicationUser> userManager,
        string email,
        string password,
        string fullName,
        Guid tenantId,
        UserRole role)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FullName = fullName,
                TenantId = tenantId,
                Role = role,
                IsActive = true,
            };

            var createResult = await userManager.CreateAsync(user, password);
            if (!createResult.Succeeded)
            {
                var errors = string.Join(", ", createResult.Errors.Select(error => error.Description));
                throw new InvalidOperationException($"Unable to create seed user {email}: {errors}");
            }
        }

        var roleName = role.ToString();
        if (!await userManager.IsInRoleAsync(user, roleName))
        {
            await userManager.AddToRoleAsync(user, roleName);
        }
    }

    private static void ApplyTenantDefaults(Tenant tenant)
    {
        if (tenant.SessionTimeoutMinutes <= 0)
        {
            tenant.SessionTimeoutMinutes = 30;
        }

        if (string.IsNullOrWhiteSpace(tenant.SubscriptionPlan))
        {
            tenant.SubscriptionPlan = "Free";
        }

        if (tenant.EstimatedSubscriptionChargeInr is null)
        {
            tenant.EstimatedSubscriptionChargeInr = 0;
        }
    }

    private static async Task SeedSubscriptionPlansAsync(AppDbContext dbContext)
    {
        var plans = new List<SubscriptionPlan>
        {
            new()
            {
                Name = "Free",
                PriceInrPerMonth = 0,
                MaxInvoicesPerMonth = 10,
                MaxUsers = 1,
                Features = "Basic invoice creation, PDF export",
                IsActive = true,
            },
            new()
            {
                Name = "Starter",
                PriceInrPerMonth = 499,
                MaxInvoicesPerMonth = 250,
                MaxUsers = 3,
                Features = "Email invoices, simple reminders, GST reports",
                IsActive = true,
            },
            new()
            {
                Name = "Growth",
                PriceInrPerMonth = 999,
                MaxInvoicesPerMonth = 1000,
                MaxUsers = 10,
                Features = "WhatsApp alerts, automated reminders, analytics dashboard",
                IsActive = true,
            },
            new()
            {
                Name = "Business",
                PriceInrPerMonth = 2000,
                MaxInvoicesPerMonth = 5000,
                MaxUsers = 25,
                Features = "Priority support, multi-user approvals, advanced exports",
                IsActive = true,
            },
            new()
            {
                Name = "Enterprise",
                PriceInrPerMonth = 5000,
                MaxInvoicesPerMonth = int.MaxValue,
                MaxUsers = int.MaxValue,
                Features = "API access, SSO readiness, dedicated success manager",
                IsActive = true,
            },
        };

        var existingPlans = await dbContext.SubscriptionPlans.ToListAsync();

        foreach (var seedPlan in plans)
        {
            var existingPlan = existingPlans.FirstOrDefault(plan => plan.Name == seedPlan.Name);
            if (existingPlan is null)
            {
                dbContext.SubscriptionPlans.Add(seedPlan);
                continue;
            }

            existingPlan.PriceInrPerMonth = seedPlan.PriceInrPerMonth;
            existingPlan.MaxInvoicesPerMonth = seedPlan.MaxInvoicesPerMonth;
            existingPlan.MaxUsers = seedPlan.MaxUsers;
            existingPlan.Features = seedPlan.Features;
            existingPlan.IsActive = true;
        }

        var activePlanNames = plans.Select(plan => plan.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var stalePlan in existingPlans.Where(plan => !activePlanNames.Contains(plan.Name)))
        {
            stalePlan.IsActive = false;
        }

        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedDemoBusinessDataAsync(AppDbContext dbContext, Guid tenantId, string demoEmail)
    {
        var demoUser = await dbContext.Users.FirstOrDefaultAsync(user => user.Email == demoEmail);
        if (demoUser is null)
        {
            return;
        }

        if (!await dbContext.Clients.AnyAsync(client => client.TenantId == tenantId))
        {
            dbContext.Clients.AddRange(
                new Client
                {
                    TenantId = tenantId,
                    Name = "Acme Retail Pvt Ltd",
                    Email = "accounts@acmeretail.in",
                    Phone = "9876543210",
                    AddressLine1 = "45 Market Road",
                    City = "Bengaluru",
                    State = "Karnataka",
                    Pincode = "560034",
                    GSTIN = "29AABCU9603R1ZX",
                    PAN = "AABCU9603R",
                    BusinessType = BusinessType.Company,
                    ContactPersonName = "Rita Sharma",
                },
                new Client
                {
                    TenantId = tenantId,
                    Name = "Zen Services LLP",
                    Email = "finance@zenservices.in",
                    Phone = "9988776655",
                    AddressLine1 = "22 Tech Park",
                    City = "Hyderabad",
                    State = "Telangana",
                    Pincode = "500081",
                    GSTIN = "36AAEFZ1234L1Z9",
                    PAN = "AAEFZ1234L",
                    BusinessType = BusinessType.LLP,
                    ContactPersonName = "Arjun Rao",
                }
            );
            await dbContext.SaveChangesAsync();
        }

        if (!await dbContext.Products.AnyAsync(product => product.TenantId == tenantId))
        {
            dbContext.Products.AddRange(
                new Product
                {
                    TenantId = tenantId,
                    Name = "GST Filing Service",
                    Description = "Monthly GST return filing",
                    HSNCode = "9983",
                    UnitPrice = 2500,
                    GSTRate = GSTRate.Eighteen,
                    Unit = "Service",
                },
                new Product
                {
                    TenantId = tenantId,
                    Name = "Accounting Support",
                    Description = "Monthly bookkeeping and reconciliations",
                    HSNCode = "9982",
                    UnitPrice = 5000,
                    GSTRate = GSTRate.Eighteen,
                    Unit = "Month",
                }
            );
            await dbContext.SaveChangesAsync();
        }

        if (!await dbContext.Invoices.AnyAsync(invoice => invoice.TenantId == tenantId))
        {
            var firstClient = await dbContext.Clients.Where(client => client.TenantId == tenantId).OrderBy(client => client.Name).FirstAsync();
            var firstProduct = await dbContext.Products.Where(product => product.TenantId == tenantId).OrderBy(product => product.Name).FirstAsync();

            var invoice = new Invoice
            {
                TenantId = tenantId,
                ClientId = firstClient.Id,
                CreatedByUserId = demoUser.Id,
                InvoiceNumber = "INV-2026-0001",
                InvoiceType = InvoiceType.TaxInvoice,
                InvoiceDate = DateTime.UtcNow.Date,
                DueDate = DateTime.UtcNow.Date.AddDays(15),
                PlaceOfSupply = firstClient.State,
                Subtotal = 2500,
                Discount = 0,
                TaxableAmount = 2500,
                TotalCGST = 225,
                TotalSGST = 225,
                TotalIGST = 0,
                GrandTotal = 2950,
                RoundOff = 0,
                Status = InvoiceStatus.Sent,
                Notes = "Thank you for your business",
                Terms = "Payment due in 15 days",
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow,
                Items =
                {
                    new InvoiceItem
                    {
                        ProductId = firstProduct.Id,
                        Description = firstProduct.Name,
                        HSNCode = firstProduct.HSNCode,
                        Quantity = 1,
                        Unit = firstProduct.Unit,
                        UnitPrice = firstProduct.UnitPrice,
                        Discount = 0,
                        GSTRate = firstProduct.GSTRate,
                        CGSTAmount = 225,
                        SGSTAmount = 225,
                        IGSTAmount = 0,
                        TotalAmount = 2950,
                    },
                },
            };

            dbContext.Invoices.Add(invoice);
            await dbContext.SaveChangesAsync();
        }

        if (!await dbContext.Notifications.AnyAsync(notification => notification.TenantId == tenantId && notification.UserId == demoUser.Id))
        {
            dbContext.Notifications.AddRange(
                new Notification
                {
                    TenantId = tenantId,
                    UserId = demoUser.Id,
                    Title = "Welcome to GST Invoice",
                    Message = "Your workspace is ready. Create your first live invoice and send it through email or WhatsApp.",
                    Type = NotificationType.System,
                    IsRead = false,
                    CreatedAtUtc = DateTime.UtcNow.AddMinutes(-30),
                },
                new Notification
                {
                    TenantId = tenantId,
                    UserId = demoUser.Id,
                    Title = "Subscription Recommendation",
                    Message = "Business plan at ₹2000/month is now available for growing teams needing higher invoice limits.",
                    Type = NotificationType.Subscription,
                    IsRead = false,
                    CreatedAtUtc = DateTime.UtcNow.AddMinutes(-10),
                }
            );

            await dbContext.SaveChangesAsync();
        }
    }
}
