namespace Billing.Api.Domain;

public sealed class PricePlan
{
    public Guid Id { get; set; }

    public Guid ProductId { get; set; }

    public Product? Product { get; set; }

    public required string BillingType { get; set; }

    public decimal Amount { get; set; }

    public required string Currency { get; set; }

    public required string BillingInterval { get; set; }

    public string? UsageUnit { get; set; }
}
