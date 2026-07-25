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
using GSTInvoice.Shared.DTOs.Product;
using GSTInvoice.Shared.Pagination;
using Microsoft.EntityFrameworkCore;

namespace GSTInvoice.API.Services;

public class ProductService(AppDbContext dbContext, ITenantContextAccessor tenantContextAccessor, ICacheService cacheService) : IProductService
{
    public async Task<PagedResult<ProductDto>> GetPagedAsync(PaginationRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContextAccessor.GetTenantId();
        var cacheKey = $"tenant:{tenantId}:products:{request.PageNumber}:{request.PageSize}:{request.Search}";
        var cachedResult = await cacheService.GetAsync<PagedResult<ProductDto>>(cacheKey, cancellationToken);
        if (cachedResult is not null)
        {
            return cachedResult;
        }

        var query = dbContext.Products.AsNoTracking().Where(product => product.TenantId == tenantId);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(product => product.Name.Contains(term) || product.HSNCode.Contains(term));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(product => product.Name)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(product => new ProductDto
            {
                Id = product.Id,
                TenantId = product.TenantId,
                Name = product.Name,
                Description = product.Description,
                HSNCode = product.HSNCode,
                DefaultUnitPrice = product.UnitPrice,
                DefaultGSTRate = product.GSTRate,
                UnitOfMeasure = product.Unit,
                CreatedAtUtc = product.CreatedAtUtc,
            })
            .ToListAsync(cancellationToken);

        var result = new PagedResult<ProductDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
        };

        await cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(10), cancellationToken);
        return result;
    }

    public async Task<ProductDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContextAccessor.GetTenantId();
        return await dbContext.Products
            .AsNoTracking()
            .Where(product => product.TenantId == tenantId && product.Id == id)
            .Select(product => new ProductDto
            {
                Id = product.Id,
                TenantId = product.TenantId,
                Name = product.Name,
                Description = product.Description,
                HSNCode = product.HSNCode,
                DefaultUnitPrice = product.UnitPrice,
                DefaultGSTRate = product.GSTRate,
                UnitOfMeasure = product.Unit,
                CreatedAtUtc = product.CreatedAtUtc,
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ProductDto> CreateAsync(UpsertProductRequestDto request, CancellationToken cancellationToken = default)
    {
        var entity = new Product
        {
            Id = Guid.NewGuid(),
            TenantId = tenantContextAccessor.GetTenantId(),
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            HSNCode = request.HSNCode.Trim(),
            UnitPrice = request.DefaultUnitPrice,
            GSTRate = request.DefaultGSTRate,
            Unit = request.UnitOfMeasure.Trim(),
            CreatedAtUtc = DateTime.UtcNow,
        };

        dbContext.Products.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return MapProductDto(entity);
    }

    public async Task<ProductDto?> UpdateAsync(Guid id, UpsertProductRequestDto request, CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContextAccessor.GetTenantId();
        var entity = await dbContext.Products.FirstOrDefaultAsync(product => product.TenantId == tenantId && product.Id == id, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        entity.Name = request.Name.Trim();
        entity.Description = request.Description?.Trim();
        entity.HSNCode = request.HSNCode.Trim();
        entity.UnitPrice = request.DefaultUnitPrice;
        entity.GSTRate = request.DefaultGSTRate;
        entity.Unit = request.UnitOfMeasure.Trim();

        await dbContext.SaveChangesAsync(cancellationToken);

        return MapProductDto(entity);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContextAccessor.GetTenantId();
        var entity = await dbContext.Products.FirstOrDefaultAsync(product => product.TenantId == tenantId && product.Id == id, cancellationToken);
        if (entity is null)
        {
            return false;
        }

        dbContext.Products.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static ProductDto MapProductDto(Product entity)
    {
        return new ProductDto
        {
            Id = entity.Id,
            TenantId = entity.TenantId,
            Name = entity.Name,
            Description = entity.Description,
            HSNCode = entity.HSNCode,
            DefaultUnitPrice = entity.UnitPrice,
            DefaultGSTRate = entity.GSTRate,
            UnitOfMeasure = entity.Unit,
            CreatedAtUtc = entity.CreatedAtUtc,
        };
    }
}

