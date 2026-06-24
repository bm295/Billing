namespace Billing.Application.Usage;

public interface IUsageService
{
    Task<UsageRecordResponse> ReportUsageAsync(
        ReportUsageRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<UsageRecordResponse>> GetUsageForBillingPeriodAsync(
        Guid subscriptionId,
        DateTimeOffset billingPeriodStart,
        DateTimeOffset billingPeriodEnd,
        CancellationToken cancellationToken);
}
