namespace Billing.Api.Contracts;

public sealed record PayInvoiceRequest(
    string? Provider = null,
    bool SimulateFailure = false);
