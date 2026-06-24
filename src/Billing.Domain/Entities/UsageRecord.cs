namespace Billing.Domain;

public sealed class UsageRecord
{
    public Guid Id { get; set; }

    public Guid CustomerId { get; set; }

    public Customer? Customer { get; set; }

    public Guid SubscriptionId { get; set; }

    public Subscription? Subscription { get; set; }

    public required string MetricName { get; set; }

    public decimal Quantity { get; set; }

    public DateTimeOffset Timestamp { get; set; }

    public required string IdempotencyKey { get; set; }
}
