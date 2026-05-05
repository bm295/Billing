using Billing.Domain;
using Microsoft.EntityFrameworkCore;

namespace Billing.Infrastructure.Persistence;

public static class BillingSeedData
{
    public static readonly Guid SampleCustomerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid ApiPlatformProductId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid ProPlanId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    public static async Task SeedAsync(BillingDbContext db, CancellationToken cancellationToken = default)
    {
        await db.Database.EnsureCreatedAsync(cancellationToken);

        if (await db.Customers.AnyAsync(cancellationToken))
        {
            return;
        }

        db.Customers.Add(new Customer
        {
            Id = SampleCustomerId,
            Email = "billing@example.com",
            CompanyName = "Example SaaS Co",
            BillingAddress = "1 Billing Street",
            PaymentMethodId = "pm_default",
            Status = CustomerStatuses.Active,
            CreatedAt = new DateTimeOffset(2026, 5, 5, 0, 0, 0, TimeSpan.Zero)
        });

        db.Products.Add(new Product
        {
            Id = ApiPlatformProductId,
            Name = "API Platform",
            Description = "Usage-based API product",
            Active = true
        });

        db.PricePlans.Add(new PricePlan
        {
            Id = ProPlanId,
            ProductId = ApiPlatformProductId,
            BillingType = BillingTypes.Recurring,
            Amount = 49m,
            Currency = "USD",
            BillingInterval = BillingIntervals.Month,
            UsageUnit = "API_CALL"
        });

        await db.SaveChangesAsync(cancellationToken);
    }
}
