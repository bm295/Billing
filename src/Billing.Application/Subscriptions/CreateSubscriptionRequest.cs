namespace Billing.Application.Subscriptions;

public sealed record CreateSubscriptionRequest(
    Guid CustomerId,
    Guid PricePlanId,
    DateOnly? StartDate = null);
