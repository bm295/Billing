namespace Billing.Application.Catalog;

public interface ICatalogService
{
    Task<ProductResponse> CreateProductAsync(
        CreateProductRequest request,
        CancellationToken cancellationToken);

    Task<ProductResponse?> GetProductAsync(
        Guid productId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ProductResponse>> ListProductsAsync(CancellationToken cancellationToken);

    Task<ProductResponse?> UpdateProductAsync(
        Guid productId,
        CreateProductRequest request,
        CancellationToken cancellationToken);

    Task<ProductResponse?> DeactivateProductAsync(
        Guid productId,
        CancellationToken cancellationToken);

    Task<PricePlanResponse> CreatePricePlanAsync(
        CreatePricePlanRequest request,
        CancellationToken cancellationToken);

    Task<PricePlanResponse?> GetPricePlanAsync(
        Guid pricePlanId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<PricePlanResponse>> ListPricePlansAsync(CancellationToken cancellationToken);

    Task<PricePlanResponse?> UpdatePricePlanAsync(
        Guid pricePlanId,
        CreatePricePlanRequest request,
        CancellationToken cancellationToken);

    Task<PricePlanResponse?> DeactivatePricePlanAsync(
        Guid pricePlanId,
        CancellationToken cancellationToken);
}
