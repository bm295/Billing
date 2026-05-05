namespace Billing.Domain;

public sealed class Customer
{
    public Guid Id { get; set; }

    public required string Email { get; set; }

    public required string CompanyName { get; set; }

    public required string BillingAddress { get; set; }

    public required string PaymentMethodId { get; set; }

    public required string Status { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
