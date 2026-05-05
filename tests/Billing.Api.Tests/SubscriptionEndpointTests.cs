using System.Net;
using System.Net.Http.Json;
using Billing.Infrastructure.Persistence;
using Billing.Domain;
using Billing.Application.Common;
using Billing.Application.Subscriptions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Billing.Api.Tests;

public sealed class SubscriptionEndpointTests : IClassFixture<BillingApiFactory>
{
    private readonly BillingApiFactory _factory;

    public SubscriptionEndpointTests(BillingApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateSubscription_CreatesActiveSubscription_ForValidCustomerAndPlan()
    {
        var client = _factory.CreateClient();
        var request = new CreateSubscriptionRequest(
            BillingSeedData.SampleCustomerId,
            BillingSeedData.ProPlanId,
            new DateOnly(2026, 5, 5));

        var httpResponse = await client.PostAsJsonAsync("/subscriptions", request);

        Assert.Equal(HttpStatusCode.Created, httpResponse.StatusCode);

        var response = await httpResponse.Content.ReadFromJsonAsync<SubscriptionResponse>();
        Assert.NotNull(response);
        Assert.Equal(request.CustomerId, response.CustomerId);
        Assert.Equal(request.PricePlanId, response.PricePlanId);
        Assert.Equal(SubscriptionStatuses.Active, response.Status);
        Assert.Equal(new DateOnly(2026, 5, 5), response.StartDate);
        Assert.Equal(new DateOnly(2026, 5, 5), response.CurrentPeriodStart);
        Assert.Equal(new DateOnly(2026, 6, 5), response.CurrentPeriodEnd);
        Assert.False(response.CancelAtPeriodEnd);
        Assert.Equal($"/subscriptions/{response.Id}", httpResponse.Headers.Location?.OriginalString);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BillingDbContext>();
        var savedSubscription = await db.Subscriptions.SingleAsync(subscription => subscription.Id == response.Id);
        Assert.Equal(SubscriptionStatuses.Active, savedSubscription.Status);
        Assert.Equal(new DateOnly(2026, 6, 5), savedSubscription.CurrentPeriodEnd);
    }

    [Fact]
    public async Task CreateSubscription_ReturnsBadRequest_WhenCustomerDoesNotExist()
    {
        var client = _factory.CreateClient();
        var request = new CreateSubscriptionRequest(Guid.NewGuid(), BillingSeedData.ProPlanId);

        var httpResponse = await client.PostAsJsonAsync("/subscriptions", request);

        Assert.Equal(HttpStatusCode.BadRequest, httpResponse.StatusCode);

        var error = await httpResponse.Content.ReadFromJsonAsync<ApiError>();
        Assert.NotNull(error);
        Assert.Equal("customer_not_found", error.Code);
    }

    [Fact]
    public async Task CreateSubscription_ReturnsBadRequest_WhenPricePlanDoesNotExist()
    {
        var client = _factory.CreateClient();
        var request = new CreateSubscriptionRequest(BillingSeedData.SampleCustomerId, Guid.NewGuid());

        var httpResponse = await client.PostAsJsonAsync("/subscriptions", request);

        Assert.Equal(HttpStatusCode.BadRequest, httpResponse.StatusCode);

        var error = await httpResponse.Content.ReadFromJsonAsync<ApiError>();
        Assert.NotNull(error);
        Assert.Equal("price_plan_not_found", error.Code);
    }
}

public sealed class BillingApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = Guid.NewGuid().ToString("N");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<BillingDbContext>>();
            services.AddDbContext<BillingDbContext>(options =>
            {
                options.UseInMemoryDatabase(_databaseName);
            });
        });
    }
}
