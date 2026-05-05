using Billing.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Billing.Api.Data;

public sealed class BillingDbContext(DbContextOptions<BillingDbContext> options) : DbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<Product> Products => Set<Product>();

    public DbSet<PricePlan> PricePlans => Set<PricePlan>();

    public DbSet<Subscription> Subscriptions => Set<Subscription>();

    public DbSet<Invoice> Invoices => Set<Invoice>();

    public DbSet<InvoiceLine> InvoiceLines => Set<InvoiceLine>();

    public DbSet<Payment> Payments => Set<Payment>();

    public DbSet<PaymentAttempt> PaymentAttempts => Set<PaymentAttempt>();

    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(customer => customer.Id);
            entity.Property(customer => customer.Email).HasMaxLength(320).IsRequired();
            entity.Property(customer => customer.CompanyName).HasMaxLength(200).IsRequired();
            entity.Property(customer => customer.BillingAddress).HasMaxLength(500).IsRequired();
            entity.Property(customer => customer.PaymentMethodId).HasMaxLength(100).IsRequired();
            entity.Property(customer => customer.Status).HasMaxLength(50).IsRequired();
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(product => product.Id);
            entity.Property(product => product.Name).HasMaxLength(200).IsRequired();
            entity.Property(product => product.Description).HasMaxLength(500).IsRequired();
        });

        modelBuilder.Entity<PricePlan>(entity =>
        {
            entity.HasKey(plan => plan.Id);
            entity.Property(plan => plan.BillingType).HasMaxLength(50).IsRequired();
            entity.Property(plan => plan.Amount).HasPrecision(18, 2);
            entity.Property(plan => plan.Currency).HasMaxLength(3).IsRequired();
            entity.Property(plan => plan.BillingInterval).HasMaxLength(20).IsRequired();
            entity.Property(plan => plan.UsageUnit).HasMaxLength(100);

            entity.HasOne(plan => plan.Product)
                .WithMany()
                .HasForeignKey(plan => plan.ProductId);
        });

        modelBuilder.Entity<Subscription>(entity =>
        {
            entity.HasKey(subscription => subscription.Id);
            entity.Property(subscription => subscription.Status).HasMaxLength(50).IsRequired();
            entity.HasIndex(subscription => subscription.CustomerId);

            entity.HasOne(subscription => subscription.Customer)
                .WithMany()
                .HasForeignKey(subscription => subscription.CustomerId);

            entity.HasOne(subscription => subscription.PricePlan)
                .WithMany()
                .HasForeignKey(subscription => subscription.PricePlanId);
        });

        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.HasKey(invoice => invoice.Id);
            entity.Property(invoice => invoice.Status).HasMaxLength(50).IsRequired();
            entity.Property(invoice => invoice.AmountDue).HasPrecision(18, 2);
            entity.Property(invoice => invoice.AmountPaid).HasPrecision(18, 2);
            entity.HasIndex(invoice => invoice.Status);
            entity.HasIndex(invoice => new { invoice.SubscriptionId, invoice.BillingPeriodStart, invoice.BillingPeriodEnd })
                .IsUnique();

            entity.HasOne(invoice => invoice.Customer)
                .WithMany()
                .HasForeignKey(invoice => invoice.CustomerId);

            entity.HasOne(invoice => invoice.Subscription)
                .WithMany()
                .HasForeignKey(invoice => invoice.SubscriptionId);

            entity.HasMany(invoice => invoice.Lines)
                .WithOne(line => line.Invoice)
                .HasForeignKey(line => line.InvoiceId);
        });

        modelBuilder.Entity<InvoiceLine>(entity =>
        {
            entity.HasKey(line => line.Id);
            entity.Property(line => line.Description).HasMaxLength(500).IsRequired();
            entity.Property(line => line.Amount).HasPrecision(18, 2);
            entity.Property(line => line.Quantity).HasPrecision(18, 4);
            entity.Property(line => line.UnitPrice).HasPrecision(18, 4);
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(payment => payment.Id);
            entity.Property(payment => payment.Amount).HasPrecision(18, 2);
            entity.Property(payment => payment.Status).HasMaxLength(50).IsRequired();
            entity.Property(payment => payment.Provider).HasMaxLength(100).IsRequired();
            entity.Property(payment => payment.IdempotencyKey).HasMaxLength(200).IsRequired();
            entity.HasIndex(payment => payment.IdempotencyKey).IsUnique();
            entity.HasIndex(payment => payment.InvoiceId);

            entity.HasOne(payment => payment.Invoice)
                .WithMany()
                .HasForeignKey(payment => payment.InvoiceId);

            entity.HasMany(payment => payment.Attempts)
                .WithOne(attempt => attempt.Payment)
                .HasForeignKey(attempt => attempt.PaymentId);
        });

        modelBuilder.Entity<PaymentAttempt>(entity =>
        {
            entity.HasKey(attempt => attempt.Id);
            entity.Property(attempt => attempt.Status).HasMaxLength(50).IsRequired();
            entity.Property(attempt => attempt.FailureReason).HasMaxLength(500);
            entity.HasIndex(attempt => new { attempt.PaymentId, attempt.AttemptNumber }).IsUnique();
        });

        modelBuilder.Entity<IdempotencyRecord>(entity =>
        {
            entity.HasKey(record => record.Id);
            entity.Property(record => record.Key).HasMaxLength(200).IsRequired();
            entity.Property(record => record.RequestHash).HasMaxLength(128).IsRequired();
            entity.Property(record => record.Status).HasMaxLength(50).IsRequired();
            entity.Property(record => record.ErrorCode).HasMaxLength(100);
            entity.Property(record => record.ErrorMessage).HasMaxLength(500);
            entity.HasIndex(record => record.Key).IsUnique();
        });
    }
}
