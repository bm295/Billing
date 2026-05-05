namespace Billing.Api.Domain;

public static class CustomerStatuses
{
    public const string Active = "active";
}

public static class SubscriptionStatuses
{
    public const string Active = "active";
}

public static class BillingTypes
{
    public const string Recurring = "recurring";
    public const string Usage = "usage";
}

public static class BillingIntervals
{
    public const string Month = "month";
    public const string Year = "year";
}

public static class InvoiceStatuses
{
    public const string Open = "open";
    public const string Paid = "paid";
    public const string Void = "void";
}

public static class PaymentStatuses
{
    public const string Succeeded = "succeeded";
    public const string Failed = "failed";
}

public static class PaymentAttemptStatuses
{
    public const string Succeeded = "succeeded";
    public const string Failed = "failed";
}

public static class IdempotencyRecordStatuses
{
    public const string InProgress = "in_progress";
    public const string Completed = "completed";
    public const string Failed = "failed";
}
