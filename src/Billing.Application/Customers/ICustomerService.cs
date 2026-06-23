namespace Billing.Application.Customers;

public interface ICustomerService
{
    Task<CustomerResponse> CreateCustomerAsync(
        CreateCustomerRequest request,
        CancellationToken cancellationToken);

    Task<CustomerResponse?> GetCustomerAsync(
        Guid customerId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<CustomerResponse>> ListCustomersAsync(CancellationToken cancellationToken);
}
