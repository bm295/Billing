using Billing.Domain;
using Billing.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Billing.Application.Customers;

public sealed class CustomerService(
    BillingDbContext db,
    TimeProvider timeProvider) : ICustomerService
{
    public async Task<CustomerResponse> CreateCustomerAsync(
        CreateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        var email = NormalizeRequired(request.Email, nameof(request.Email));

        var emailExists = await db.Customers.AnyAsync(
            customer => customer.Email == email,
            cancellationToken);

        if (emailExists)
        {
            throw new InvalidOperationException($"Customer email '{email}' is already registered.");
        }

        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            Email = email,
            CompanyName = NormalizeRequired(request.CompanyName, nameof(request.CompanyName)),
            BillingAddress = NormalizeRequired(request.BillingAddress, nameof(request.BillingAddress)),
            PaymentMethodId = NormalizeRequired(request.PaymentMethodId, nameof(request.PaymentMethodId)),
            Status = CustomerStatuses.Active,
            CreatedAt = timeProvider.GetUtcNow()
        };

        db.Customers.Add(customer);
        await db.SaveChangesAsync(cancellationToken);

        return CustomerResponse.FromEntity(customer);
    }

    public async Task<CustomerResponse?> GetCustomerAsync(
        Guid customerId,
        CancellationToken cancellationToken)
    {
        var customer = await db.Customers.SingleOrDefaultAsync(
            item => item.Id == customerId,
            cancellationToken);

        return customer is null ? null : CustomerResponse.FromEntity(customer);
    }

    public async Task<IReadOnlyList<CustomerResponse>> ListCustomersAsync(CancellationToken cancellationToken)
    {
        var customers = await db.Customers
            .OrderBy(customer => customer.CreatedAt)
            .ToArrayAsync(cancellationToken);

        return customers
            .Select(CustomerResponse.FromEntity)
            .ToArray();
    }

    private static string NormalizeRequired(string value, string parameterName)
    {
        var normalized = value.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ArgumentException("Value cannot be empty.", parameterName);
        }

        return normalized;
    }
}
