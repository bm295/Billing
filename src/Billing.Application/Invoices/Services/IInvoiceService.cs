using Billing.Application.Invoices.Results;

namespace Billing.Application.Invoices.Services;

public interface IInvoiceService
{
    Task<InvoiceGenerationResult> GenerateRecurringInvoiceAsync(
        Guid subscriptionId,
        CancellationToken cancellationToken);
}
