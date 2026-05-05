using System.Net;
using System.Net.Http.Json;
using Billing.Api.Contracts;
using Billing.Api.Data;
using Billing.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Billing.Api.Tests;

public sealed class InvoiceEndpointTests : IClassFixture<BillingApiFactory>
{
    private readonly BillingApiFactory _factory;

    public InvoiceEndpointTests(BillingApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GenerateInvoice_CreatesOpenInvoiceWithRecurringPlanLine()
    {
        var client = _factory.CreateClient();
        var subscription = await CreateSubscriptionAsync(client);
        var request = new GenerateInvoiceRequest(subscription.Id);

        var httpResponse = await client.PostAsJsonAsync("/invoices/generate", request);

        Assert.Equal(HttpStatusCode.Created, httpResponse.StatusCode);

        var response = await httpResponse.Content.ReadFromJsonAsync<InvoiceResponse>();
        Assert.NotNull(response);
        Assert.Equal(subscription.CustomerId, response.CustomerId);
        Assert.Equal(subscription.Id, response.SubscriptionId);
        Assert.Equal(InvoiceStatuses.Open, response.Status);
        Assert.Equal(49m, response.AmountDue);
        Assert.Equal(0m, response.AmountPaid);
        Assert.Equal(subscription.CurrentPeriodStart, response.BillingPeriodStart);
        Assert.Equal(subscription.CurrentPeriodEnd, response.BillingPeriodEnd);
        Assert.Equal(subscription.CurrentPeriodEnd, response.DueDate);
        Assert.Equal($"/invoices/{response.Id}", httpResponse.Headers.Location?.OriginalString);

        var line = Assert.Single(response.Lines);
        Assert.Equal("API Platform monthly subscription", line.Description);
        Assert.Equal(49m, line.Amount);
        Assert.Equal(1m, line.Quantity);
        Assert.Equal(49m, line.UnitPrice);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BillingDbContext>();
        var savedInvoice = await db.Invoices
            .Include(invoice => invoice.Lines)
            .SingleAsync(invoice => invoice.Id == response.Id);
        Assert.Equal(49m, savedInvoice.AmountDue);
        Assert.Single(savedInvoice.Lines);
    }

    [Fact]
    public async Task GenerateInvoice_ReturnsBadRequest_WhenSubscriptionDoesNotExist()
    {
        var client = _factory.CreateClient();
        var request = new GenerateInvoiceRequest(Guid.NewGuid());

        var httpResponse = await client.PostAsJsonAsync("/invoices/generate", request);

        Assert.Equal(HttpStatusCode.BadRequest, httpResponse.StatusCode);

        var error = await httpResponse.Content.ReadFromJsonAsync<ApiError>();
        Assert.NotNull(error);
        Assert.Equal("subscription_not_found", error.Code);
    }

    [Fact]
    public async Task GenerateInvoice_ReturnsExistingInvoice_ForSameSubscriptionPeriod()
    {
        var client = _factory.CreateClient();
        var subscription = await CreateSubscriptionAsync(client);
        var request = new GenerateInvoiceRequest(subscription.Id);

        var firstHttpResponse = await client.PostAsJsonAsync("/invoices/generate", request);
        var secondHttpResponse = await client.PostAsJsonAsync("/invoices/generate", request);

        Assert.Equal(HttpStatusCode.Created, firstHttpResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondHttpResponse.StatusCode);

        var first = await firstHttpResponse.Content.ReadFromJsonAsync<InvoiceResponse>();
        var second = await secondHttpResponse.Content.ReadFromJsonAsync<InvoiceResponse>();
        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal(first.Id, second.Id);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BillingDbContext>();
        var invoiceCount = await db.Invoices.CountAsync(invoice => invoice.SubscriptionId == subscription.Id);
        Assert.Equal(1, invoiceCount);
    }

    [Fact]
    public async Task GenerateInvoice_CreatesSingleInvoice_ForConcurrentRequests()
    {
        var client = _factory.CreateClient();
        var subscription = await CreateSubscriptionAsync(client);
        var request = new GenerateInvoiceRequest(subscription.Id);

        var httpResponses = await Task.WhenAll(
            Enumerable.Range(0, 8)
                .Select(_ => client.PostAsJsonAsync("/invoices/generate", request)));

        Assert.All(httpResponses, response =>
        {
            Assert.True(
                response.StatusCode is HttpStatusCode.Created or HttpStatusCode.OK,
                $"Expected Created or OK, got {response.StatusCode}.");
        });

        var invoices = await Task.WhenAll(
            httpResponses.Select(async response =>
            {
                var invoice = await response.Content.ReadFromJsonAsync<InvoiceResponse>();
                Assert.NotNull(invoice);
                return invoice;
            }));

        var invoiceId = Assert.Single(invoices.Select(invoice => invoice.Id).Distinct());
        Assert.NotEqual(Guid.Empty, invoiceId);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BillingDbContext>();
        var invoiceCount = await db.Invoices.CountAsync(invoice => invoice.SubscriptionId == subscription.Id);
        Assert.Equal(1, invoiceCount);
    }

    private static async Task<SubscriptionResponse> CreateSubscriptionAsync(HttpClient client)
    {
        var request = new CreateSubscriptionRequest(
            BillingSeedData.SampleCustomerId,
            BillingSeedData.ProPlanId,
            new DateOnly(2026, 5, 5));

        var httpResponse = await client.PostAsJsonAsync("/subscriptions", request);
        httpResponse.EnsureSuccessStatusCode();

        var subscription = await httpResponse.Content.ReadFromJsonAsync<SubscriptionResponse>();
        Assert.NotNull(subscription);
        return subscription;
    }
}
