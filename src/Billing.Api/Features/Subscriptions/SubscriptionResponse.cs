using Billing.Api.Domain;

namespace Billing.Api.Features.Subscriptions;

public sealed record SubscriptionResponse(
    Guid Id,
    Guid CustomerId,
    Guid PricePlanId,
    string Status,
    DateOnly StartDate,
    DateOnly CurrentPeriodStart,
    DateOnly CurrentPeriodEnd,
    bool CancelAtPeriodEnd)
{
    public static SubscriptionResponse FromEntity(Subscription subscription)
    {
        return new SubscriptionResponse(
            subscription.Id,
            subscription.CustomerId,
            subscription.PricePlanId,
            subscription.Status,
            subscription.StartDate,
            subscription.CurrentPeriodStart,
            subscription.CurrentPeriodEnd,
            subscription.CancelAtPeriodEnd);
    }
}
