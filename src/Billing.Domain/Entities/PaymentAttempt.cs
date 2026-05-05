namespace Billing.Domain;

public sealed class PaymentAttempt
{
    public Guid Id { get; set; }

    public Guid PaymentId { get; set; }

    public Payment? Payment { get; set; }

    public int AttemptNumber { get; set; }

    public required string Status { get; set; }

    public string? FailureReason { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
