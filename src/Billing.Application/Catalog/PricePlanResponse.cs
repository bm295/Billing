using Billing.Domain;

namespace Billing.Application.Catalog;

public sealed record PricePlanResponse(
    Guid Id,
    Guid ProductId,
    string BillingType,
    decimal Amount,
    string Currency,
    string BillingInterval,
    string? UsageUnit,
    bool Active)
{
    public static PricePlanResponse FromEntity(PricePlan pricePlan)
    {
        return new PricePlanResponse(
            pricePlan.Id,
            pricePlan.ProductId,
            pricePlan.BillingType,
            pricePlan.Amount,
            pricePlan.Currency,
            pricePlan.BillingInterval,
            pricePlan.UsageUnit,
            pricePlan.Active);
    }
}
