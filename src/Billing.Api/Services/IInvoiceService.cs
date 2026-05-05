namespace Billing.Api.Services;

public interface IInvoiceService
{
    Task<InvoiceGenerationResult> GenerateRecurringInvoiceAsync(
        Guid subscriptionId,
        CancellationToken cancellationToken);
}
