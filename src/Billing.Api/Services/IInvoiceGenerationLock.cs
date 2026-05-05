namespace Billing.Api.Services;

public interface IInvoiceGenerationLock
{
    Task<IDisposable> AcquireAsync(
        Guid subscriptionId,
        DateOnly billingPeriodStart,
        DateOnly billingPeriodEnd,
        CancellationToken cancellationToken);
}
