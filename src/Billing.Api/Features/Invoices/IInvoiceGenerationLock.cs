namespace Billing.Api.Features.Invoices;

public interface IInvoiceGenerationLock
{
    Task<IDisposable> AcquireAsync(
        Guid subscriptionId,
        DateOnly billingPeriodStart,
        DateOnly billingPeriodEnd,
        CancellationToken cancellationToken);
}
