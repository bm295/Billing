using Billing.Domain;

namespace Billing.Application.Invoices.Contracts;

public sealed record InvoiceLineResponse(
    Guid Id,
    string Description,
    decimal Amount,
    decimal Quantity,
    decimal UnitPrice)
{
    public static InvoiceLineResponse FromEntity(InvoiceLine line)
    {
        return new InvoiceLineResponse(
            line.Id,
            line.Description,
            line.Amount,
            line.Quantity,
            line.UnitPrice);
    }
}
