using Billing.Api.Domain;

namespace Billing.Api.Contracts;

public sealed record InvoiceResponse(
    Guid Id,
    Guid CustomerId,
    Guid SubscriptionId,
    string Status,
    decimal AmountDue,
    decimal AmountPaid,
    DateOnly BillingPeriodStart,
    DateOnly BillingPeriodEnd,
    DateOnly DueDate,
    DateTimeOffset CreatedAt,
    IReadOnlyList<InvoiceLineResponse> Lines)
{
    public static InvoiceResponse FromEntity(Invoice invoice)
    {
        return new InvoiceResponse(
            invoice.Id,
            invoice.CustomerId,
            invoice.SubscriptionId,
            invoice.Status,
            invoice.AmountDue,
            invoice.AmountPaid,
            invoice.BillingPeriodStart,
            invoice.BillingPeriodEnd,
            invoice.DueDate,
            invoice.CreatedAt,
            invoice.Lines.Select(InvoiceLineResponse.FromEntity).ToArray());
    }
}
