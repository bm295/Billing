namespace Billing.Application.Subscriptions;

public interface ISubscriptionService
{
    Task<SubscriptionResponse> CreateSubscriptionAsync(
        CreateSubscriptionRequest request,
        CancellationToken cancellationToken);

    Task<SubscriptionResponse?> GetSubscriptionAsync(
        Guid subscriptionId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<SubscriptionResponse>> ListCustomerSubscriptionsAsync(
        Guid customerId,
        CancellationToken cancellationToken);

    Task<SubscriptionResponse?> CancelSubscriptionAsync(
        Guid subscriptionId,
        CancelSubscriptionRequest request,
        CancellationToken cancellationToken);

    Task<ChangeSubscriptionPlanResponse?> ChangeSubscriptionPlanAsync(
        Guid subscriptionId,
        ChangeSubscriptionPlanRequest request,
        CancellationToken cancellationToken);
}
