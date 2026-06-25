using Billing.Domain;
using Billing.Application.Invoices.Results;

namespace Billing.Application.Invoices.Services;

public interface IInvoiceService
{
    Task<InvoiceGenerationResult> GenerateRecurringInvoiceAsync(
        Guid subscriptionId,
        CancellationToken cancellationToken);

    Task<Invoice?> GetInvoiceAsync(
        Guid invoiceId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Invoice>> ListCustomerInvoicesAsync(
        Guid customerId,
        CancellationToken cancellationToken);
}
