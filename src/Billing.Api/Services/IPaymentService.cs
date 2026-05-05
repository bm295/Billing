using Billing.Api.Contracts;

namespace Billing.Api.Services;

public interface IPaymentService
{
    Task<PaymentResult> PayInvoiceAsync(
        Guid invoiceId,
        PayInvoiceRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken);
}
