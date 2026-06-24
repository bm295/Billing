using Billing.Domain;

namespace Billing.Application.Usage;

public sealed record UsageRecordResponse(
    Guid Id,
    Guid CustomerId,
    Guid SubscriptionId,
    string MetricName,
    decimal Quantity,
    DateTimeOffset Timestamp,
    string IdempotencyKey)
{
    public static UsageRecordResponse FromEntity(UsageRecord usageRecord)
    {
        return new UsageRecordResponse(
            usageRecord.Id,
            usageRecord.CustomerId,
            usageRecord.SubscriptionId,
            usageRecord.MetricName,
            usageRecord.Quantity,
            usageRecord.Timestamp,
            usageRecord.IdempotencyKey);
    }
}
