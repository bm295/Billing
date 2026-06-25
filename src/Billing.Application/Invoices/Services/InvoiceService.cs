using Billing.Infrastructure.Persistence;
using Billing.Domain;
using Billing.Domain.Concurrency;
using Billing.Application.Invoices.Results;
using Microsoft.EntityFrameworkCore;

namespace Billing.Application.Invoices.Services;

public sealed class InvoiceService(
    BillingDbContext db,
    TimeProvider timeProvider,
    IKeyedLock keyedLock) : IInvoiceService
{
    public async Task<Invoice?> GetInvoiceAsync(
        Guid invoiceId,
        CancellationToken cancellationToken)
    {
        return await db.Invoices
            .Include(invoice => invoice.Lines)
            .SingleOrDefaultAsync(
                invoice => invoice.Id == invoiceId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Invoice>> ListCustomerInvoicesAsync(
        Guid customerId,
        CancellationToken cancellationToken)
    {
        return await db.Invoices
            .Include(invoice => invoice.Lines)
            .Where(invoice => invoice.CustomerId == customerId)
            .OrderByDescending(invoice => invoice.CreatedAt)
            .ThenByDescending(invoice => invoice.Id)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<InvoiceGenerationResult> GenerateRecurringInvoiceAsync(
        Guid subscriptionId,
        CancellationToken cancellationToken)
    {
        var subscription = await db.Subscriptions
            .Include(item => item.PricePlan)
            .ThenInclude(plan => plan!.Product)
            .SingleOrDefaultAsync(
                item => item.Id == subscriptionId
                    && item.Status == SubscriptionStatuses.Active,
                cancellationToken);

        if (subscription is null)
        {
            return InvoiceGenerationResult.Failed(
                "subscription_not_found",
                "Subscription does not exist or is not active.");
        }

        if (subscription.PricePlan is null)
        {
            return InvoiceGenerationResult.Failed(
                "price_plan_not_found",
                "Subscription price plan does not exist.");
        }

        if (subscription.PricePlan.BillingType != BillingTypes.Recurring)
        {
            return InvoiceGenerationResult.Failed(
                "billing_type_not_supported",
                $"Billing type '{subscription.PricePlan.BillingType}' is not supported for invoice generation.");
        }

        var invoiceLockKey =
            $"invoice:{subscription.Id:N}:{subscription.CurrentPeriodStart:yyyyMMdd}:{subscription.CurrentPeriodEnd:yyyyMMdd}";
        using var lease = await keyedLock.AcquireAsync(invoiceLockKey, cancellationToken);

        return await CreateOrGetInvoiceAsync(subscription, cancellationToken);
    }

    private async Task<InvoiceGenerationResult> CreateOrGetInvoiceAsync(
        Subscription subscription,
        CancellationToken cancellationToken)
    {
        var existingInvoice = await FindInvoiceForPeriodAsync(subscription, cancellationToken);
        if (existingInvoice is not null)
        {
            return InvoiceGenerationResult.Existing(existingInvoice);
        }

        var invoice = BuildInvoice(subscription);
        db.Invoices.Add(invoice);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return InvoiceGenerationResult.Created(invoice);
        }
        catch (DbUpdateException)
        {
            DetachInvoice(invoice);

            existingInvoice = await FindInvoiceForPeriodAsync(subscription, cancellationToken);
            if (existingInvoice is not null)
            {
                return InvoiceGenerationResult.Existing(existingInvoice);
            }

            throw;
        }
    }

    private async Task<Invoice?> FindInvoiceForPeriodAsync(
        Subscription subscription,
        CancellationToken cancellationToken)
    {
        return await db.Invoices
            .Include(invoice => invoice.Lines)
            .SingleOrDefaultAsync(
                invoice => invoice.SubscriptionId == subscription.Id
                    && invoice.BillingPeriodStart == subscription.CurrentPeriodStart
                    && invoice.BillingPeriodEnd == subscription.CurrentPeriodEnd,
                cancellationToken);
    }

    private Invoice BuildInvoice(Subscription subscription)
    {
        var plan = subscription.PricePlan
            ?? throw new InvalidOperationException("Subscription price plan must be loaded before invoice generation.");

        var planAmount = plan.Amount;
        var line = new InvoiceLine
        {
            Id = Guid.NewGuid(),
            Description = BuildLineDescription(plan),
            Quantity = 1m,
            UnitPrice = planAmount,
            Amount = planAmount
        };

        return new Invoice
        {
            Id = Guid.NewGuid(),
            CustomerId = subscription.CustomerId,
            SubscriptionId = subscription.Id,
            Status = InvoiceStatuses.Open,
            AmountDue = line.Amount,
            AmountPaid = 0m,
            BillingPeriodStart = subscription.CurrentPeriodStart,
            BillingPeriodEnd = subscription.CurrentPeriodEnd,
            DueDate = subscription.CurrentPeriodEnd,
            CreatedAt = timeProvider.GetUtcNow(),
            Lines = [line]
        };
    }

    private static string BuildLineDescription(PricePlan plan)
    {
        var productName = plan.Product?.Name ?? "Subscription";
        return plan.BillingInterval switch
        {
            BillingIntervals.Month => $"{productName} monthly subscription",
            BillingIntervals.Year => $"{productName} yearly subscription",
            _ => $"{productName} subscription"
        };
    }

    private void DetachInvoice(Invoice invoice)
    {
        db.Entry(invoice).State = EntityState.Detached;

        foreach (var line in invoice.Lines)
        {
            db.Entry(line).State = EntityState.Detached;
        }
    }
}
