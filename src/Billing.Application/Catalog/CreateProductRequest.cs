namespace Billing.Application.Catalog;

public sealed record CreateProductRequest(
    string Name,
    string Description,
    bool Active);
