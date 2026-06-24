namespace Billing.Application.Usage;

public sealed record ReportUsageRequest(
    Guid SubscriptionId,
    string MetricName,
    decimal Quantity,
    DateTimeOffset Timestamp);
