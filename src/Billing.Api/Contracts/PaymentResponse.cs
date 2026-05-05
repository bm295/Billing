using Billing.Api.Domain;

namespace Billing.Api.Contracts;

public sealed record PaymentResponse(
    Guid Id,
    Guid InvoiceId,
    decimal Amount,
    string Status,
    string Provider,
    string IdempotencyKey,
    DateTimeOffset CreatedAt,
    IReadOnlyList<PaymentAttemptResponse> Attempts)
{
    public static PaymentResponse FromEntity(Payment payment)
    {
        return new PaymentResponse(
            payment.Id,
            payment.InvoiceId,
            payment.Amount,
            payment.Status,
            payment.Provider,
            payment.IdempotencyKey,
            payment.CreatedAt,
            payment.Attempts.Select(PaymentAttemptResponse.FromEntity).ToArray());
    }
}
