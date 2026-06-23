using Billing.Domain;

namespace Billing.Application.Catalog;

public sealed record ProductResponse(
    Guid Id,
    string Name,
    string Description,
    bool Active)
{
    public static ProductResponse FromEntity(Product product)
    {
        return new ProductResponse(
            product.Id,
            product.Name,
            product.Description,
            product.Active);
    }
}
