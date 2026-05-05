namespace Billing.Application.Payments;

public interface IPaymentService
{
    Task<PaymentResult> PayInvoiceAsync(
        Guid invoiceId,
        PayInvoiceRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken);
}
