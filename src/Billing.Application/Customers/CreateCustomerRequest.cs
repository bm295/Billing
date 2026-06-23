namespace Billing.Application.Customers;

public sealed record CreateCustomerRequest(
    string Email,
    string CompanyName,
    string BillingAddress,
    string PaymentMethodId);
