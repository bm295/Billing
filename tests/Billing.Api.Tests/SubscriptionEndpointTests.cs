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
    public async Task GetSubscription_ReturnsSubscription_WhenSubscriptionExists()
    {
        var client = _factory.CreateClient();
        var created = await CreateSubscriptionAsync(client, new DateOnly(2026, 7, 1));

        var httpResponse = await client.GetAsync($"/subscriptions/{created.Id}");

        Assert.Equal(HttpStatusCode.OK, httpResponse.StatusCode);

        var response = await httpResponse.Content.ReadFromJsonAsync<SubscriptionResponse>();
        Assert.NotNull(response);
        Assert.Equal(created.Id, response.Id);
        Assert.Equal(created.CustomerId, response.CustomerId);
        Assert.Equal(created.PricePlanId, response.PricePlanId);
        Assert.Equal(created.CurrentPeriodStart, response.CurrentPeriodStart);
        Assert.Equal(created.CurrentPeriodEnd, response.CurrentPeriodEnd);
    }

    [Fact]
    public async Task GetSubscription_ReturnsNotFound_WhenSubscriptionDoesNotExist()
    {
        var client = _factory.CreateClient();

        var httpResponse = await client.GetAsync($"/subscriptions/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, httpResponse.StatusCode);

        var error = await httpResponse.Content.ReadFromJsonAsync<ApiError>();
        Assert.NotNull(error);
        Assert.Equal("subscription_not_found", error.Code);
    }

    [Fact]
    public async Task ListCustomerSubscriptions_ReturnsCustomerSubscriptions()
    {
        var client = _factory.CreateClient();
        var first = await CreateSubscriptionAsync(client, new DateOnly(2026, 8, 1));
        var second = await CreateSubscriptionAsync(client, new DateOnly(2026, 9, 1));

        var httpResponse = await client.GetAsync($"/customers/{BillingSeedData.SampleCustomerId}/subscriptions");

        Assert.Equal(HttpStatusCode.OK, httpResponse.StatusCode);

        var response = await httpResponse.Content.ReadFromJsonAsync<SubscriptionResponse[]>();
        Assert.NotNull(response);
        Assert.Contains(response, subscription => subscription.Id == first.Id);
        Assert.Contains(response, subscription => subscription.Id == second.Id);
        Assert.All(response, subscription => Assert.Equal(BillingSeedData.SampleCustomerId, subscription.CustomerId));
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
    public void CancelSubscriptionRequest_StoresCancelAtPeriodEnd()
    {
        var request = new CancelSubscriptionRequest(CancelAtPeriodEnd: true);

        Assert.True(request.CancelAtPeriodEnd);
    }


    [Fact]
    public async Task CancelSubscription_CancelsImmediately_WhenCancelAtPeriodEndIsFalse()
    {
        var client = _factory.CreateClient();
        var created = await CreateSubscriptionAsync(client, new DateOnly(2026, 10, 1));
        var request = new CancelSubscriptionRequest(CancelAtPeriodEnd: false);

        var httpResponse = await client.PostAsJsonAsync($"/subscriptions/{created.Id}/cancel", request);

        Assert.Equal(HttpStatusCode.OK, httpResponse.StatusCode);

        var response = await httpResponse.Content.ReadFromJsonAsync<SubscriptionResponse>();
        Assert.NotNull(response);
        Assert.Equal(created.Id, response.Id);
        Assert.Equal(SubscriptionStatuses.Canceled, response.Status);
        Assert.False(response.CancelAtPeriodEnd);
    }

    [Fact]
    public async Task CancelSubscription_MarksCancelAtPeriodEnd_WhenRequested()
    {
        var client = _factory.CreateClient();
        var created = await CreateSubscriptionAsync(client, new DateOnly(2026, 10, 1));
        var request = new CancelSubscriptionRequest(CancelAtPeriodEnd: true);

        var httpResponse = await client.PostAsJsonAsync($"/subscriptions/{created.Id}/cancel", request);

        Assert.Equal(HttpStatusCode.OK, httpResponse.StatusCode);

        var response = await httpResponse.Content.ReadFromJsonAsync<SubscriptionResponse>();
        Assert.NotNull(response);
        Assert.Equal(SubscriptionStatuses.Active, response.Status);
        Assert.True(response.CancelAtPeriodEnd);
    }

    [Fact]
    public async Task ChangeSubscriptionPlan_UpdatesPlanAndReturnsProration_ForActiveSubscription()
    {
        var client = _factory.CreateClient();
        var premiumPlanId = await CreateMonthlyPlanAsync(109m);
        var created = await CreateSubscriptionAsync(client, new DateOnly(2026, 6, 1));
        var request = new ChangeSubscriptionPlanRequest(premiumPlanId);

        var httpResponse = await client.PostAsJsonAsync($"/subscriptions/{created.Id}/change-plan", request);

        Assert.Equal(HttpStatusCode.OK, httpResponse.StatusCode);

        var response = await httpResponse.Content.ReadFromJsonAsync<ChangeSubscriptionPlanResponse>();
        Assert.NotNull(response);
        Assert.Equal(created.Id, response.Subscription.Id);
        Assert.Equal(premiumPlanId, response.Subscription.PricePlanId);
        Assert.Equal(new DateOnly(2026, 6, 1), response.Subscription.CurrentPeriodStart);
        Assert.Equal(new DateOnly(2026, 7, 1), response.Subscription.CurrentPeriodEnd);
        Assert.False(response.Subscription.CancelAtPeriodEnd);
        Assert.Equal(9.80m, response.Proration.UnusedCredit);
        Assert.Equal(21.80m, response.Proration.RemainingPlanCost);
        Assert.Equal(12.00m, response.Proration.NetAmountDue);
    }

    [Fact]
    public async Task ChangeSubscriptionPlan_ReturnsBadRequest_WhenSubscriptionIsCanceled()
    {
        var client = _factory.CreateClient();
        var premiumPlanId = await CreateMonthlyPlanAsync(109m);
        var created = await CreateSubscriptionAsync(client, new DateOnly(2026, 12, 1));
        await client.PostAsJsonAsync(
            $"/subscriptions/{created.Id}/cancel",
            new CancelSubscriptionRequest(CancelAtPeriodEnd: false));

        var httpResponse = await client.PostAsJsonAsync(
            $"/subscriptions/{created.Id}/change-plan",
            new ChangeSubscriptionPlanRequest(premiumPlanId));

        Assert.Equal(HttpStatusCode.BadRequest, httpResponse.StatusCode);

        var error = await httpResponse.Content.ReadFromJsonAsync<ApiError>();
        Assert.NotNull(error);
        Assert.Equal("subscription_not_active", error.Code);
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

    private async Task<Guid> CreateMonthlyPlanAsync(decimal amount)
    {
        var pricePlanId = Guid.NewGuid();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BillingDbContext>();
        db.PricePlans.Add(new PricePlan
        {
            Id = pricePlanId,
            ProductId = BillingSeedData.ApiPlatformProductId,
            BillingType = BillingTypes.Recurring,
            Amount = amount,
            Currency = "USD",
            BillingInterval = BillingIntervals.Month,
            UsageUnit = "API_CALL",
            Active = true
        });

        await db.SaveChangesAsync();

        return pricePlanId;
    }

    private static async Task<SubscriptionResponse> CreateSubscriptionAsync(HttpClient client, DateOnly startDate)
    {
        var request = new CreateSubscriptionRequest(
            BillingSeedData.SampleCustomerId,
            BillingSeedData.ProPlanId,
            startDate);

        var httpResponse = await client.PostAsJsonAsync("/subscriptions", request);

        Assert.Equal(HttpStatusCode.Created, httpResponse.StatusCode);

        var response = await httpResponse.Content.ReadFromJsonAsync<SubscriptionResponse>();
        Assert.NotNull(response);
        return response;
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
