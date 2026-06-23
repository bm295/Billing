using Billing.Domain;

namespace Billing.Application.Customers;

public sealed record CustomerResponse(
    Guid Id,
    string Email,
    string CompanyName,
    string BillingAddress,
    string PaymentMethodId,
    string Status,
    DateTimeOffset CreatedAt)
{
    public static CustomerResponse FromEntity(Customer customer)
    {
        return new CustomerResponse(
            customer.Id,
            customer.Email,
            customer.CompanyName,
            customer.BillingAddress,
            customer.PaymentMethodId,
            customer.Status,
            customer.CreatedAt);
    }
}
