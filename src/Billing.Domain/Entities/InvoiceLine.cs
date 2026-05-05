namespace Billing.Domain;

public sealed class InvoiceLine
{
    public Guid Id { get; set; }

    public Guid InvoiceId { get; set; }

    public Invoice? Invoice { get; set; }

    public required string Description { get; set; }

    public decimal Amount { get; set; }

    public decimal Quantity { get; set; }

    public decimal UnitPrice { get; set; }
}
