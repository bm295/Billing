using Billing.Infrastructure.Persistence;
using Billing.Domain;
using Billing.Application.Common;
using Billing.Application.Subscriptions;
using Microsoft.EntityFrameworkCore;

namespace Billing.Api.Endpoints;

public static class SubscriptionEndpoints
{
    public static IEndpointRouteBuilder MapSubscriptionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/subscriptions");

        group.MapPost("", CreateSubscriptionAsync)
            .WithName("CreateSubscription");

        group.MapGet("/{subscriptionId:guid}", GetSubscriptionAsync)
            .WithName("GetSubscription");

        app.MapGet("/customers/{customerId:guid}/subscriptions", ListCustomerSubscriptionsAsync)
            .WithName("ListCustomerSubscriptions");

        return app;
    }

    private static async Task<IResult> CreateSubscriptionAsync(
        CreateSubscriptionRequest request,
        BillingDbContext db,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var customerExists = await db.Customers
            .AnyAsync(
                customer => customer.Id == request.CustomerId
                    && customer.Status == CustomerStatuses.Active,
                cancellationToken);

        if (!customerExists)
        {
            return Results.BadRequest(new ApiError(
                "customer_not_found",
                "Customer does not exist or is not active."));
        }

        var pricePlan = await db.PricePlans
            .SingleOrDefaultAsync(
                plan => plan.Id == request.PricePlanId && plan.Active,
                cancellationToken);

        if (pricePlan is null)
        {
            return Results.BadRequest(new ApiError(
                "price_plan_not_found",
                "Price plan does not exist."));
        }

        var startDate = request.StartDate
            ?? DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

        var currentPeriodEnd = CalculateCurrentPeriodEnd(startDate, pricePlan.BillingInterval);
        if (currentPeriodEnd is null)
        {
            return Results.BadRequest(new ApiError(
                "billing_interval_not_supported",
                $"Billing interval '{pricePlan.BillingInterval}' is not supported."));
        }

        var subscription = new Subscription
        {
            Id = Guid.NewGuid(),
            CustomerId = request.CustomerId,
            PricePlanId = request.PricePlanId,
            Status = SubscriptionStatuses.Active,
            StartDate = startDate,
            CurrentPeriodStart = startDate,
            CurrentPeriodEnd = currentPeriodEnd.Value,
            CancelAtPeriodEnd = false
        };

        db.Subscriptions.Add(subscription);
        await db.SaveChangesAsync(cancellationToken);

        var response = SubscriptionResponse.FromEntity(subscription);
        return Results.Created($"/subscriptions/{subscription.Id}", response);
    }

    private static async Task<IResult> GetSubscriptionAsync(
        Guid subscriptionId,
        BillingDbContext db,
        CancellationToken cancellationToken)
    {
        var subscription = await db.Subscriptions
            .SingleOrDefaultAsync(
                item => item.Id == subscriptionId,
                cancellationToken);

        return subscription is null
            ? Results.NotFound(new ApiError("subscription_not_found", "Subscription does not exist."))
            : Results.Ok(SubscriptionResponse.FromEntity(subscription));
    }

    private static async Task<IResult> ListCustomerSubscriptionsAsync(
        Guid customerId,
        BillingDbContext db,
        CancellationToken cancellationToken)
    {
        var subscriptions = await db.Subscriptions
            .Where(subscription => subscription.CustomerId == customerId)
            .OrderBy(subscription => subscription.StartDate)
            .ThenBy(subscription => subscription.Id)
            .ToArrayAsync(cancellationToken);

        var response = subscriptions
            .Select(SubscriptionResponse.FromEntity)
            .ToArray();

        return Results.Ok(response);
    }

    private static DateOnly? CalculateCurrentPeriodEnd(DateOnly currentPeriodStart, string billingInterval)
    {
        return billingInterval switch
        {
            BillingIntervals.Month => currentPeriodStart.AddMonths(1),
            BillingIntervals.Year => currentPeriodStart.AddYears(1),
            _ => null
        };
    }
}
