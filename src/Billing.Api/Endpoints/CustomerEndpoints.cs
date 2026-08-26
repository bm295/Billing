using Billing.Api.Contracts;
using Billing.Application.Customers;

namespace Billing.Api.Endpoints;

public static class CustomerEndpoints
{
    public static IEndpointRouteBuilder MapCustomerEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/customers");

        group.MapPost("", CreateCustomerAsync)
            .WithName("CreateCustomer");

        group.MapGet("/{customerId:guid}", GetCustomerAsync)
            .WithName("GetCustomer");

        group.MapGet("", ListCustomersAsync)
            .WithName("ListCustomers");

        return app;
    }

    private static async Task<IResult> CreateCustomerAsync(
        CreateCustomerRequest request,
        ICustomerService customerService,
        CancellationToken cancellationToken)
    {
        try
        {
            var customer = await customerService.CreateCustomerAsync(request, cancellationToken);
            return Results.Created($"/customers/{customer.Id}", customer);
        }
        catch (ArgumentException exception)
        {
            return Results.BadRequest(new ApiError("invalid_customer", exception.Message));
        }
        catch (InvalidOperationException exception)
        {
            return Results.Conflict(new ApiError("customer_email_already_exists", exception.Message));
        }
    }

    private static async Task<IResult> GetCustomerAsync(
        Guid customerId,
        ICustomerService customerService,
        CancellationToken cancellationToken)
    {
        var customer = await customerService.GetCustomerAsync(customerId, cancellationToken);
        if (customer is null)
        {
            return Results.NotFound(new ApiError("customer_not_found", "Customer does not exist."));
        }

        return Results.Ok(customer);
    }

    private static async Task<IResult> ListCustomersAsync(
        ICustomerService customerService,
        CancellationToken cancellationToken)
    {
        var customers = await customerService.ListCustomersAsync(cancellationToken);
        return Results.Ok(customers);
    }
}
