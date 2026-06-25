using Billing.Domain;
using Billing.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Billing.Application.Subscriptions;

public sealed class SubscriptionService(
    BillingDbContext db,
    TimeProvider timeProvider,
    ProrationCalculator prorationCalculator) : ISubscriptionService
{
    public async Task<SubscriptionResponse> CreateSubscriptionAsync(
        CreateSubscriptionRequest request,
        CancellationToken cancellationToken)
    {
        var customerExists = await db.Customers
            .AnyAsync(
                customer => customer.Id == request.CustomerId
                    && customer.Status == CustomerStatuses.Active,
                cancellationToken);

        if (!customerExists)
        {
            throw new InvalidOperationException("Customer does not exist or is not active.");
        }

        var pricePlan = await GetActivePricePlanAsync(request.PricePlanId, cancellationToken);
        if (pricePlan is null)
        {
            throw new ArgumentException("Price plan does not exist.", nameof(request.PricePlanId));
        }

        var startDate = request.StartDate
            ?? DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

        var currentPeriodEnd = CalculateCurrentPeriodEnd(startDate, pricePlan.BillingInterval);
        if (currentPeriodEnd is null)
        {
            throw new NotSupportedException($"Billing interval '{pricePlan.BillingInterval}' is not supported.");
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

        return SubscriptionResponse.FromEntity(subscription);
    }

    public async Task<SubscriptionResponse?> GetSubscriptionAsync(
        Guid subscriptionId,
        CancellationToken cancellationToken)
    {
        var subscription = await db.Subscriptions
            .SingleOrDefaultAsync(
                item => item.Id == subscriptionId,
                cancellationToken);

        return subscription is null ? null : SubscriptionResponse.FromEntity(subscription);
    }

    public async Task<IReadOnlyList<SubscriptionResponse>> ListCustomerSubscriptionsAsync(
        Guid customerId,
        CancellationToken cancellationToken)
    {
        var subscriptions = await db.Subscriptions
            .Where(subscription => subscription.CustomerId == customerId)
            .OrderBy(subscription => subscription.StartDate)
            .ThenBy(subscription => subscription.Id)
            .ToArrayAsync(cancellationToken);

        return subscriptions
            .Select(SubscriptionResponse.FromEntity)
            .ToArray();
    }

    public async Task<SubscriptionResponse?> CancelSubscriptionAsync(
        Guid subscriptionId,
        CancelSubscriptionRequest request,
        CancellationToken cancellationToken)
    {
        var subscription = await db.Subscriptions
            .SingleOrDefaultAsync(
                item => item.Id == subscriptionId,
                cancellationToken);

        if (subscription is null)
        {
            return null;
        }

        if (request.CancelAtPeriodEnd)
        {
            subscription.CancelAtPeriodEnd = true;
        }
        else
        {
            subscription.Status = SubscriptionStatuses.Canceled;
            subscription.CancelAtPeriodEnd = false;
        }

        await db.SaveChangesAsync(cancellationToken);

        return SubscriptionResponse.FromEntity(subscription);
    }

    public async Task<ChangeSubscriptionPlanResponse?> ChangeSubscriptionPlanAsync(
        Guid subscriptionId,
        ChangeSubscriptionPlanRequest request,
        CancellationToken cancellationToken)
    {
        var subscription = await db.Subscriptions
            .Include(item => item.PricePlan)
            .SingleOrDefaultAsync(
                item => item.Id == subscriptionId,
                cancellationToken);

        if (subscription is null)
        {
            return null;
        }

        if (subscription.Status != SubscriptionStatuses.Active)
        {
            throw new InvalidOperationException("Only active subscriptions can change plans.");
        }

        var pricePlan = await GetActivePricePlanAsync(request.PricePlanId, cancellationToken);
        if (pricePlan is null)
        {
            throw new ArgumentException("Price plan does not exist.", nameof(request.PricePlanId));
        }

        var currentPricePlan = subscription.PricePlan
            ?? throw new InvalidOperationException("Subscription does not have a current price plan.");
        var changeDate = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var proration = prorationCalculator.Calculate(
            currentPricePlan.Amount,
            pricePlan.Amount,
            subscription.CurrentPeriodStart,
            subscription.CurrentPeriodEnd,
            changeDate);

        subscription.PricePlanId = request.PricePlanId;
        subscription.PricePlan = pricePlan;
        subscription.CancelAtPeriodEnd = false;

        await db.SaveChangesAsync(cancellationToken);

        return new ChangeSubscriptionPlanResponse(
            SubscriptionResponse.FromEntity(subscription),
            proration);
    }

    private Task<PricePlan?> GetActivePricePlanAsync(Guid pricePlanId, CancellationToken cancellationToken)
    {
        return db.PricePlans
            .SingleOrDefaultAsync(
                plan => plan.Id == pricePlanId && plan.Active,
                cancellationToken);
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
