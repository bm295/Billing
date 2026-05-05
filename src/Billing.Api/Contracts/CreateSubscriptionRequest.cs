namespace Billing.Api.Contracts;

public sealed record CreateSubscriptionRequest(
    Guid CustomerId,
    Guid PricePlanId,
    DateOnly? StartDate = null);
