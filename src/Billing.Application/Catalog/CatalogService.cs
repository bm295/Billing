using Billing.Domain;
using Billing.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Billing.Application.Catalog;

public sealed class CatalogService(BillingDbContext db) : ICatalogService
{
    public async Task<ProductResponse> CreateProductAsync(
        CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = NormalizeRequired(request.Name, nameof(request.Name)),
            Description = NormalizeRequired(request.Description, nameof(request.Description)),
            Active = request.Active
        };

        db.Products.Add(product);
        await db.SaveChangesAsync(cancellationToken);

        return ProductResponse.FromEntity(product);
    }

    public async Task<ProductResponse?> GetProductAsync(
        Guid productId,
        CancellationToken cancellationToken)
    {
        var product = await db.Products.SingleOrDefaultAsync(
            item => item.Id == productId,
            cancellationToken);

        return product is null ? null : ProductResponse.FromEntity(product);
    }

    public async Task<IReadOnlyList<ProductResponse>> ListProductsAsync(CancellationToken cancellationToken)
    {
        var products = await db.Products
            .OrderBy(product => product.Name)
            .ThenBy(product => product.Id)
            .ToArrayAsync(cancellationToken);

        return products
            .Select(ProductResponse.FromEntity)
            .ToArray();
    }

    public async Task<ProductResponse?> UpdateProductAsync(
        Guid productId,
        CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        var product = await db.Products.SingleOrDefaultAsync(
            item => item.Id == productId,
            cancellationToken);

        if (product is null)
        {
            return null;
        }

        product.Name = NormalizeRequired(request.Name, nameof(request.Name));
        product.Description = NormalizeRequired(request.Description, nameof(request.Description));
        product.Active = request.Active;

        await db.SaveChangesAsync(cancellationToken);

        return ProductResponse.FromEntity(product);
    }

    public async Task<ProductResponse?> DeactivateProductAsync(
        Guid productId,
        CancellationToken cancellationToken)
    {
        var product = await db.Products.SingleOrDefaultAsync(
            item => item.Id == productId,
            cancellationToken);

        if (product is null)
        {
            return null;
        }

        product.Active = false;
        await db.SaveChangesAsync(cancellationToken);

        return ProductResponse.FromEntity(product);
    }

    public async Task<PricePlanResponse> CreatePricePlanAsync(
        CreatePricePlanRequest request,
        CancellationToken cancellationToken)
    {
        await EnsureActiveProductExistsAsync(request.ProductId, cancellationToken);

        var pricePlan = new PricePlan
        {
            Id = Guid.NewGuid(),
            ProductId = request.ProductId,
            BillingType = NormalizeRequired(request.BillingType, nameof(request.BillingType)),
            Amount = request.Amount,
            Currency = NormalizeRequired(request.Currency, nameof(request.Currency)).ToUpperInvariant(),
            BillingInterval = NormalizeRequired(request.BillingInterval, nameof(request.BillingInterval)),
            UsageUnit = NormalizeOptional(request.UsageUnit),
            Active = true
        };

        db.PricePlans.Add(pricePlan);
        await db.SaveChangesAsync(cancellationToken);

        return PricePlanResponse.FromEntity(pricePlan);
    }

    public async Task<PricePlanResponse?> GetPricePlanAsync(
        Guid pricePlanId,
        CancellationToken cancellationToken)
    {
        var pricePlan = await db.PricePlans.SingleOrDefaultAsync(
            item => item.Id == pricePlanId,
            cancellationToken);

        return pricePlan is null ? null : PricePlanResponse.FromEntity(pricePlan);
    }

    public async Task<IReadOnlyList<PricePlanResponse>> ListPricePlansAsync(CancellationToken cancellationToken)
    {
        var pricePlans = await db.PricePlans
            .OrderBy(plan => plan.ProductId)
            .ThenBy(plan => plan.BillingType)
            .ThenBy(plan => plan.Id)
            .ToArrayAsync(cancellationToken);

        return pricePlans
            .Select(PricePlanResponse.FromEntity)
            .ToArray();
    }

    public async Task<PricePlanResponse?> UpdatePricePlanAsync(
        Guid pricePlanId,
        CreatePricePlanRequest request,
        CancellationToken cancellationToken)
    {
        var pricePlan = await db.PricePlans.SingleOrDefaultAsync(
            item => item.Id == pricePlanId,
            cancellationToken);

        if (pricePlan is null)
        {
            return null;
        }

        await EnsureActiveProductExistsAsync(request.ProductId, cancellationToken);

        pricePlan.ProductId = request.ProductId;
        pricePlan.BillingType = NormalizeRequired(request.BillingType, nameof(request.BillingType));
        pricePlan.Amount = request.Amount;
        pricePlan.Currency = NormalizeRequired(request.Currency, nameof(request.Currency)).ToUpperInvariant();
        pricePlan.BillingInterval = NormalizeRequired(request.BillingInterval, nameof(request.BillingInterval));
        pricePlan.UsageUnit = NormalizeOptional(request.UsageUnit);

        await db.SaveChangesAsync(cancellationToken);

        return PricePlanResponse.FromEntity(pricePlan);
    }

    public async Task<PricePlanResponse?> DeactivatePricePlanAsync(
        Guid pricePlanId,
        CancellationToken cancellationToken)
    {
        var pricePlan = await db.PricePlans.SingleOrDefaultAsync(
            item => item.Id == pricePlanId,
            cancellationToken);

        if (pricePlan is null)
        {
            return null;
        }

        pricePlan.Active = false;
        await db.SaveChangesAsync(cancellationToken);

        return PricePlanResponse.FromEntity(pricePlan);
    }

    private async Task EnsureActiveProductExistsAsync(Guid productId, CancellationToken cancellationToken)
    {
        var productExists = await db.Products.AnyAsync(
            product => product.Id == productId && product.Active,
            cancellationToken);

        if (!productExists)
        {
            throw new InvalidOperationException("Product does not exist or is not active.");
        }
    }

    private static string NormalizeRequired(string value, string parameterName)
    {
        var normalized = value.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ArgumentException("Value cannot be empty.", parameterName);
        }

        return normalized;
    }

    private static string? NormalizeOptional(string? value)
    {
        if (value is null)
        {
            return null;
        }

        var normalized = value.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}
