namespace Billing.Api.Features.Payments;

public sealed record PayInvoiceRequest(
    string? Provider = null,
    bool SimulateFailure = false);
