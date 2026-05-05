namespace Billing.Domain;

public sealed class IdempotencyRecord
{
    public Guid Id { get; set; }

    public required string Key { get; set; }

    public required string RequestHash { get; set; }

    public required string Status { get; set; }

    public int? StatusCode { get; set; }

    public Guid? ResponsePaymentId { get; set; }

    public string? ErrorCode { get; set; }

    public string? ErrorMessage { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
