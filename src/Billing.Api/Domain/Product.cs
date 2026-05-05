namespace Billing.Api.Domain;

public sealed class Product
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public required string Description { get; set; }

    public bool Active { get; set; }
}
