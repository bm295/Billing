namespace Billing.Application.Subscriptions;

public sealed record ChangeSubscriptionPlanResponse(
    SubscriptionResponse Subscription,
    ProrationResult Proration);
