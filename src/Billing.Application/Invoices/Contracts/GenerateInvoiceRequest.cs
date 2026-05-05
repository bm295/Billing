namespace Billing.Application.Invoices.Contracts;

public sealed record GenerateInvoiceRequest(Guid SubscriptionId);
