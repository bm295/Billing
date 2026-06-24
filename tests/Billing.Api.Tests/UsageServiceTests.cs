using Billing.Application.Usage;
using Billing.Domain;
using Billing.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

namespace Billing.Api.Tests;

public sealed class UsageServiceTests : IClassFixture<BillingApiFactory>
{
    private readonly BillingApiFactory _factory;

    public UsageServiceTests(BillingApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ReportUsageAsync_CreatesUsageRecord_ForSubscription()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BillingDbContext>();
        var usageService = scope.ServiceProvider.GetRequiredService<IUsageService>();
        var subscription = await CreateSubscriptionAsync(db);
        var request = new ReportUsageRequest(
            subscription.Id,
            "API_CALL",
            25m,
            new DateTimeOffset(2026, 6, 1, 12, 0, 0, TimeSpan.Zero));

        var response = await usageService.ReportUsageAsync(request, " usage-key-1 ", CancellationToken.None);

        Assert.Equal(subscription.CustomerId, response.CustomerId);
        Assert.Equal(subscription.Id, response.SubscriptionId);
        Assert.Equal("API_CALL", response.MetricName);
        Assert.Equal(25m, response.Quantity);
        Assert.Equal(request.Timestamp, response.Timestamp);
        Assert.Equal("usage-key-1", response.IdempotencyKey);

        var savedRecord = await db.UsageRecords.SingleAsync(record => record.Id == response.Id);
        Assert.Equal(response.IdempotencyKey, savedRecord.IdempotencyKey);
    }

    [Fact]
    public async Task ReportUsageAsync_ReturnsExistingUsageRecord_ForDuplicateIdempotencyKey()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BillingDbContext>();
        var usageService = scope.ServiceProvider.GetRequiredService<IUsageService>();
        var subscription = await CreateSubscriptionAsync(db);
        var firstRequest = new ReportUsageRequest(
            subscription.Id,
            "API_CALL",
            10m,
            new DateTimeOffset(2026, 6, 2, 12, 0, 0, TimeSpan.Zero));
        var secondRequest = firstRequest with { Quantity = 99m };

        var firstResponse = await usageService.ReportUsageAsync(firstRequest, "usage-key-2", CancellationToken.None);
        var secondResponse = await usageService.ReportUsageAsync(secondRequest, "usage-key-2", CancellationToken.None);

        Assert.Equal(firstResponse.Id, secondResponse.Id);
        Assert.Equal(10m, secondResponse.Quantity);
        Assert.Equal(1, await db.UsageRecords.CountAsync(record => record.IdempotencyKey == "usage-key-2"));
    }

    [Fact]
    public async Task GetUsageForBillingPeriodAsync_ReturnsMatchingUsageRecords()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BillingDbContext>();
        var usageService = scope.ServiceProvider.GetRequiredService<IUsageService>();
        var subscription = await CreateSubscriptionAsync(db);
        var matchingRequest = new ReportUsageRequest(
            subscription.Id,
            "API_CALL",
            15m,
            new DateTimeOffset(2026, 6, 10, 12, 0, 0, TimeSpan.Zero));
        var outsidePeriodRequest = matchingRequest with
        {
            Timestamp = new DateTimeOffset(2026, 7, 1, 0, 0, 0, TimeSpan.Zero)
        };

        var matchingResponse = await usageService.ReportUsageAsync(matchingRequest, "usage-key-3", CancellationToken.None);
        await usageService.ReportUsageAsync(outsidePeriodRequest, "usage-key-4", CancellationToken.None);

        var response = await usageService.GetUsageForBillingPeriodAsync(
            subscription.Id,
            new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 7, 1, 0, 0, 0, TimeSpan.Zero),
            CancellationToken.None);

        Assert.Contains(response, record => record.Id == matchingResponse.Id);
        Assert.DoesNotContain(response, record => record.IdempotencyKey == "usage-key-4");
    }

    private static async Task<Subscription> CreateSubscriptionAsync(BillingDbContext db)
    {
        var subscription = new Subscription
        {
            Id = Guid.NewGuid(),
            CustomerId = BillingSeedData.SampleCustomerId,
            PricePlanId = BillingSeedData.ProPlanId,
            Status = SubscriptionStatuses.Active,
            StartDate = new DateOnly(2026, 6, 1),
            CurrentPeriodStart = new DateOnly(2026, 6, 1),
            CurrentPeriodEnd = new DateOnly(2026, 7, 1),
            CancelAtPeriodEnd = false
        };

        db.Subscriptions.Add(subscription);
        await db.SaveChangesAsync();
        return subscription;
    }
}
