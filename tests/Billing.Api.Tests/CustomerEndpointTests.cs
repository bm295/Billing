using System.Net;
using System.Net.Http.Json;
using Billing.Api.Contracts;
using Billing.Application.Customers;
using Billing.Domain;
using Billing.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Billing.Api.Tests;

public sealed class CustomerEndpointTests : IClassFixture<BillingApiFactory>
{
    private readonly BillingApiFactory _factory;

    public CustomerEndpointTests(BillingApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateCustomer_CreatesActiveCustomer()
    {
        var client = _factory.CreateClient();
        var request = CreateRequest("create");

        var httpResponse = await client.PostAsJsonAsync("/customers", request);

        Assert.Equal(HttpStatusCode.Created, httpResponse.StatusCode);

        var response = await httpResponse.Content.ReadFromJsonAsync<CustomerResponse>();
        Assert.NotNull(response);
        Assert.Equal(request.Email, response.Email);
        Assert.Equal(request.CompanyName, response.CompanyName);
        Assert.Equal(request.BillingAddress, response.BillingAddress);
        Assert.Equal(request.PaymentMethodId, response.PaymentMethodId);
        Assert.Equal(CustomerStatuses.Active, response.Status);
        Assert.Equal($"/customers/{response.Id}", httpResponse.Headers.Location?.OriginalString);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BillingDbContext>();
        var savedCustomer = await db.Customers.SingleAsync(customer => customer.Id == response.Id);
        Assert.Equal(request.Email, savedCustomer.Email);
        Assert.Equal(CustomerStatuses.Active, savedCustomer.Status);
    }

    [Fact]
    public async Task GetCustomer_ReturnsCustomer_WhenCustomerExists()
    {
        var client = _factory.CreateClient();
        var created = await CreateCustomerAsync(client, "fetch");

        var httpResponse = await client.GetAsync($"/customers/{created.Id}");

        Assert.Equal(HttpStatusCode.OK, httpResponse.StatusCode);

        var response = await httpResponse.Content.ReadFromJsonAsync<CustomerResponse>();
        Assert.NotNull(response);
        Assert.Equal(created.Id, response.Id);
        Assert.Equal(created.Email, response.Email);
        Assert.Equal(created.CompanyName, response.CompanyName);
        Assert.Equal(created.BillingAddress, response.BillingAddress);
        Assert.Equal(created.PaymentMethodId, response.PaymentMethodId);
        Assert.Equal(created.Status, response.Status);
    }

    [Fact]
    public async Task ListCustomers_ReturnsCreatedCustomers()
    {
        var client = _factory.CreateClient();
        var first = await CreateCustomerAsync(client, "list-1");
        var second = await CreateCustomerAsync(client, "list-2");

        var httpResponse = await client.GetAsync("/customers");

        Assert.Equal(HttpStatusCode.OK, httpResponse.StatusCode);

        var response = await httpResponse.Content.ReadFromJsonAsync<CustomerResponse[]>();
        Assert.NotNull(response);
        Assert.Contains(response, customer => customer.Id == first.Id && customer.Email == first.Email);
        Assert.Contains(response, customer => customer.Id == second.Id && customer.Email == second.Email);
    }

    [Fact]
    public async Task CreateCustomer_ReturnsConflict_WhenEmailAlreadyExists()
    {
        var client = _factory.CreateClient();
        var request = CreateRequest("duplicate");

        var firstHttpResponse = await client.PostAsJsonAsync("/customers", request);
        var secondHttpResponse = await client.PostAsJsonAsync("/customers", request);

        Assert.Equal(HttpStatusCode.Created, firstHttpResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, secondHttpResponse.StatusCode);

        var error = await secondHttpResponse.Content.ReadFromJsonAsync<ApiError>();
        Assert.NotNull(error);
        Assert.Equal("customer_email_already_exists", error.Code);
    }

    private static async Task<CustomerResponse> CreateCustomerAsync(HttpClient client, string emailPrefix)
    {
        var httpResponse = await client.PostAsJsonAsync("/customers", CreateRequest(emailPrefix));

        Assert.Equal(HttpStatusCode.Created, httpResponse.StatusCode);

        var response = await httpResponse.Content.ReadFromJsonAsync<CustomerResponse>();
        Assert.NotNull(response);
        return response;
    }

    private static CreateCustomerRequest CreateRequest(string emailPrefix)
    {
        var uniqueSuffix = Guid.NewGuid().ToString("N");
        return new CreateCustomerRequest(
            $"{emailPrefix}-{uniqueSuffix}@example.com",
            $"{emailPrefix} Company",
            "123 Billing Street",
            $"pm_{uniqueSuffix}");
    }
}
