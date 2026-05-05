namespace Billing.Api.Domain;

public sealed class Payment
{
    public Guid Id { get; set; }

    public Guid InvoiceId { get; set; }

    public Invoice? Invoice { get; set; }

    public decimal Amount { get; set; }

    public required string Status { get; set; }

    public required string Provider { get; set; }

    public required string IdempotencyKey { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public List<PaymentAttempt> Attempts { get; set; } = [];
}
