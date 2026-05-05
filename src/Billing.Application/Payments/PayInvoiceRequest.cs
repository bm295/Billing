namespace Billing.Application.Payments;

public sealed record PayInvoiceRequest(
    string? Provider = null,
    bool SimulateFailure = false);
