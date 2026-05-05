namespace Billing.Api.Features.Invoices;

public interface IInvoiceService
{
    Task<InvoiceGenerationResult> GenerateRecurringInvoiceAsync(
        Guid subscriptionId,
        CancellationToken cancellationToken);
}
