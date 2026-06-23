namespace Billing.Application.Catalog;

public sealed record CreatePricePlanRequest(
    Guid ProductId,
    string BillingType,
    decimal Amount,
    string Currency,
    string BillingInterval,
    string? UsageUnit);
