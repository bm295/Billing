using Billing.Domain;
using Billing.Domain.Concurrency;
using Billing.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Billing.Application.Usage;

public sealed class UsageService(
    BillingDbContext db,
    IKeyedLock keyedLock) : IUsageService
{
    public async Task<UsageRecordResponse> ReportUsageAsync(
        ReportUsageRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var normalizedIdempotencyKey = NormalizeRequired(idempotencyKey, nameof(idempotencyKey));
        var normalizedMetricName = NormalizeRequired(request.MetricName, nameof(request.MetricName));

        if (request.Quantity < 0m)
        {
            throw new ArgumentException("Quantity cannot be negative.", nameof(request.Quantity));
        }

        using var lease = await keyedLock.AcquireAsync(
            $"usage-report:{normalizedIdempotencyKey}",
            cancellationToken);

        var existingUsageRecord = await FindByIdempotencyKeyAsync(normalizedIdempotencyKey, cancellationToken);
        if (existingUsageRecord is not null)
        {
            return UsageRecordResponse.FromEntity(existingUsageRecord);
        }

        var subscription = await db.Subscriptions
            .SingleOrDefaultAsync(
                item => item.Id == request.SubscriptionId,
                cancellationToken);

        if (subscription is null)
        {
            throw new InvalidOperationException("Subscription does not exist.");
        }

        var usageRecord = new UsageRecord
        {
            Id = Guid.NewGuid(),
            CustomerId = subscription.CustomerId,
            SubscriptionId = subscription.Id,
            MetricName = normalizedMetricName,
            Quantity = request.Quantity,
            Timestamp = request.Timestamp,
            IdempotencyKey = normalizedIdempotencyKey
        };

        db.UsageRecords.Add(usageRecord);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            db.Entry(usageRecord).State = EntityState.Detached;

            existingUsageRecord = await FindByIdempotencyKeyAsync(normalizedIdempotencyKey, cancellationToken);
            if (existingUsageRecord is not null)
            {
                return UsageRecordResponse.FromEntity(existingUsageRecord);
            }

            throw;
        }

        return UsageRecordResponse.FromEntity(usageRecord);
    }

    public async Task<IReadOnlyList<UsageRecordResponse>> GetUsageForBillingPeriodAsync(
        Guid subscriptionId,
        DateTimeOffset billingPeriodStart,
        DateTimeOffset billingPeriodEnd,
        CancellationToken cancellationToken)
    {
        var usageRecords = await db.UsageRecords
            .Where(record => record.SubscriptionId == subscriptionId
                && record.Timestamp >= billingPeriodStart
                && record.Timestamp < billingPeriodEnd)
            .OrderBy(record => record.Timestamp)
            .ThenBy(record => record.Id)
            .ToArrayAsync(cancellationToken);

        return usageRecords
            .Select(UsageRecordResponse.FromEntity)
            .ToArray();
    }

    private async Task<UsageRecord?> FindByIdempotencyKeyAsync(
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        return await db.UsageRecords
            .SingleOrDefaultAsync(record => record.IdempotencyKey == idempotencyKey, cancellationToken);
    }

    private static string NormalizeRequired(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be empty.", parameterName);
        }

        return value.Trim();
    }
}
