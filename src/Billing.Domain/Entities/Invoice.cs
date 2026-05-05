namespace Billing.Domain;

public sealed class Invoice
{
    public Guid Id { get; set; }

    public Guid CustomerId { get; set; }

    public Customer? Customer { get; set; }

    public Guid SubscriptionId { get; set; }

    public Subscription? Subscription { get; set; }

    public required string Status { get; set; }

    public decimal AmountDue { get; set; }

    public decimal AmountPaid { get; set; }

    public DateOnly BillingPeriodStart { get; set; }

    public DateOnly BillingPeriodEnd { get; set; }

    public DateOnly DueDate { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public List<InvoiceLine> Lines { get; set; } = [];
}
