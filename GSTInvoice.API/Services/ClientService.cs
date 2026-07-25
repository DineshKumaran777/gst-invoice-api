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
using GSTInvoice.Shared.DTOs.Client;
using GSTInvoice.Shared.Pagination;
using Microsoft.EntityFrameworkCore;

namespace GSTInvoice.API.Services;

public class ClientService(
    AppDbContext dbContext,
    ITenantContextAccessor tenantContextAccessor,
    ICacheService cacheService)
    : IClientService
{
    public async Task<PagedResult<ClientDto>> GetPagedAsync(PaginationRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContextAccessor.GetTenantId();
        var cacheKey = $"tenant:{tenantId}:clients:{request.PageNumber}:{request.PageSize}:{request.Search}";
        var cachedResult = await cacheService.GetAsync<PagedResult<ClientDto>>(cacheKey, cancellationToken);
        if (cachedResult is not null)
        {
            return cachedResult;
        }

        var query = dbContext.Clients
            .AsNoTracking()
            .Where(client => client.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(client =>
                client.Name.Contains(search) ||
                (client.Email != null && client.Email.Contains(search)) ||
                (client.Phone != null && client.Phone.Contains(search)) ||
                (client.GSTIN != null && client.GSTIN.Contains(search)));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(client => client.Name)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(client => new ClientDto
            {
                Id = client.Id,
                TenantId = client.TenantId,
                Name = client.Name,
                Email = client.Email,
                Phone = client.Phone,
                AlternatePhone = client.AlternatePhone,
                AddressLine1 = client.AddressLine1,
                AddressLine2 = client.AddressLine2,
                City = client.City,
                State = client.State,
                Pincode = client.Pincode,
                Country = client.Country,
                GSTIN = client.GSTIN,
                PAN = client.PAN,
                BusinessType = client.BusinessType,
                ContactPersonName = client.ContactPersonName,
                CreatedAtUtc = client.CreatedAtUtc,
            })
            .ToListAsync(cancellationToken);

        var result = new PagedResult<ClientDto>
        {
            Items = items,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalCount = totalCount,
        };

        await cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5), cancellationToken);
        return result;
    }

    public async Task<ClientDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContextAccessor.GetTenantId();
        var entity = await dbContext.Clients
            .AsNoTracking()
            .Where(client => client.TenantId == tenantId && client.Id == id)
            .Select(client => new ClientDto
            {
                Id = client.Id,
                TenantId = client.TenantId,
                Name = client.Name,
                Email = client.Email,
                Phone = client.Phone,
                AlternatePhone = client.AlternatePhone,
                AddressLine1 = client.AddressLine1,
                AddressLine2 = client.AddressLine2,
                City = client.City,
                State = client.State,
                Pincode = client.Pincode,
                Country = client.Country,
                GSTIN = client.GSTIN,
                PAN = client.PAN,
                BusinessType = client.BusinessType,
                ContactPersonName = client.ContactPersonName,
                CreatedAtUtc = client.CreatedAtUtc,
            })
            .FirstOrDefaultAsync(cancellationToken);

        return entity;
    }

    public async Task<ClientDto> CreateAsync(UpsertClientRequestDto request, CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContextAccessor.GetTenantId();
        var entity = new Client
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = request.Name.Trim(),
            Email = request.Email?.Trim(),
            Phone = request.Phone?.Trim(),
            AlternatePhone = request.AlternatePhone?.Trim(),
            AddressLine1 = request.AddressLine1.Trim(),
            AddressLine2 = request.AddressLine2?.Trim(),
            City = request.City.Trim(),
            State = request.State.Trim(),
            Pincode = request.Pincode.Trim(),
            Country = request.Country.Trim(),
            GSTIN = request.GSTIN?.Trim().ToUpperInvariant(),
            PAN = request.PAN?.Trim().ToUpperInvariant(),
            BusinessType = request.BusinessType,
            ContactPersonName = request.ContactPersonName?.Trim(),
            CreatedAtUtc = DateTime.UtcNow,
        };

        dbContext.Clients.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        await cacheService.RemoveAsync($"tenant:{tenantId}:clients", cancellationToken);

        return MapClientDto(entity);
    }

    public async Task<ClientDto?> UpdateAsync(Guid id, UpsertClientRequestDto request, CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContextAccessor.GetTenantId();
        var entity = await dbContext.Clients.FirstOrDefaultAsync(client => client.TenantId == tenantId && client.Id == id, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        entity.Name = request.Name.Trim();
        entity.Email = request.Email?.Trim();
        entity.Phone = request.Phone?.Trim();
        entity.AlternatePhone = request.AlternatePhone?.Trim();
        entity.AddressLine1 = request.AddressLine1.Trim();
        entity.AddressLine2 = request.AddressLine2?.Trim();
        entity.City = request.City.Trim();
        entity.State = request.State.Trim();
        entity.Pincode = request.Pincode.Trim();
        entity.Country = request.Country.Trim();
        entity.GSTIN = request.GSTIN?.Trim().ToUpperInvariant();
        entity.PAN = request.PAN?.Trim().ToUpperInvariant();
        entity.BusinessType = request.BusinessType;
        entity.ContactPersonName = request.ContactPersonName?.Trim();

        await dbContext.SaveChangesAsync(cancellationToken);
        await cacheService.RemoveAsync($"tenant:{tenantId}:clients", cancellationToken);

        return MapClientDto(entity);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContextAccessor.GetTenantId();
        var entity = await dbContext.Clients.FirstOrDefaultAsync(client => client.TenantId == tenantId && client.Id == id, cancellationToken);
        if (entity is null)
        {
            return false;
        }

        var hasInvoices = await dbContext.Invoices.AnyAsync(invoice => invoice.TenantId == tenantId && invoice.ClientId == id, cancellationToken);
        if (hasInvoices)
        {
            throw new InvalidOperationException("Cannot delete client with associated invoices.");
        }

        dbContext.Clients.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        await cacheService.RemoveAsync($"tenant:{tenantId}:clients", cancellationToken);

        return true;
    }

    private static ClientDto MapClientDto(Client entity)
    {
        return new ClientDto
        {
            Id = entity.Id,
            TenantId = entity.TenantId,
            Name = entity.Name,
            Email = entity.Email,
            Phone = entity.Phone,
            AlternatePhone = entity.AlternatePhone,
            AddressLine1 = entity.AddressLine1,
            AddressLine2 = entity.AddressLine2,
            City = entity.City,
            State = entity.State,
            Pincode = entity.Pincode,
            Country = entity.Country,
            GSTIN = entity.GSTIN,
            PAN = entity.PAN,
            BusinessType = entity.BusinessType,
            ContactPersonName = entity.ContactPersonName,
            CreatedAtUtc = entity.CreatedAtUtc,
        };
    }
}

