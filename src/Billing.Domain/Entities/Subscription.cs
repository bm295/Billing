namespace Billing.Domain;

public sealed class Subscription
{
    public Guid Id { get; set; }

    public Guid CustomerId { get; set; }

    public Customer? Customer { get; set; }

    public Guid PricePlanId { get; set; }

    public PricePlan? PricePlan { get; set; }

    public required string Status { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly CurrentPeriodStart { get; set; }

    public DateOnly CurrentPeriodEnd { get; set; }

    public bool CancelAtPeriodEnd { get; set; }
}
