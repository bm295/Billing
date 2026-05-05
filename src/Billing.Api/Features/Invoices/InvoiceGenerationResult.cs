using Billing.Api.Domain;

namespace Billing.Api.Features.Invoices;

public sealed record InvoiceGenerationResult(
    InvoiceGenerationOutcome Outcome,
    Invoice? Invoice,
    string? ErrorCode = null,
    string? ErrorMessage = null)
{
    public static InvoiceGenerationResult Created(Invoice invoice)
    {
        return new InvoiceGenerationResult(InvoiceGenerationOutcome.Created, invoice);
    }

    public static InvoiceGenerationResult Existing(Invoice invoice)
    {
        return new InvoiceGenerationResult(InvoiceGenerationOutcome.Existing, invoice);
    }

    public static InvoiceGenerationResult Failed(string errorCode, string errorMessage)
    {
        return new InvoiceGenerationResult(InvoiceGenerationOutcome.Failed, null, errorCode, errorMessage);
    }
}
