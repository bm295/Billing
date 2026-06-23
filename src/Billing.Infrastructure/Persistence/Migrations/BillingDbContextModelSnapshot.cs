using System;
using Billing.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace Billing.Infrastructure.Persistence.Migrations;

[DbContext(typeof(BillingDbContext))]
partial class BillingDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
#pragma warning disable 612, 618
        modelBuilder
            .HasAnnotation("ProductVersion", "10.0.3")
            .HasAnnotation("Relational:MaxIdentifierLength", 128);

        SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

        modelBuilder.Entity("Billing.Domain.Customer", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedOnAdd().HasColumnType("uniqueidentifier");
            b.Property<string>("BillingAddress").IsRequired().HasMaxLength(500).HasColumnType("nvarchar(500)");
            b.Property<string>("CompanyName").IsRequired().HasMaxLength(200).HasColumnType("nvarchar(200)");
            b.Property<DateTimeOffset>("CreatedAt").HasColumnType("datetimeoffset");
            b.Property<string>("Email").IsRequired().HasMaxLength(320).HasColumnType("nvarchar(320)");
            b.Property<string>("PaymentMethodId").IsRequired().HasMaxLength(100).HasColumnType("nvarchar(100)");
            b.Property<string>("Status").IsRequired().HasMaxLength(50).HasColumnType("nvarchar(50)");
            b.HasKey("Id");
            b.ToTable("Customers");
        });

        modelBuilder.Entity("Billing.Domain.IdempotencyRecord", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedOnAdd().HasColumnType("uniqueidentifier");
            b.Property<DateTimeOffset>("CreatedAt").HasColumnType("datetimeoffset");
            b.Property<string>("ErrorCode").HasMaxLength(100).HasColumnType("nvarchar(100)");
            b.Property<string>("ErrorMessage").HasMaxLength(500).HasColumnType("nvarchar(500)");
            b.Property<string>("Key").IsRequired().HasMaxLength(200).HasColumnType("nvarchar(200)");
            b.Property<string>("RequestHash").IsRequired().HasMaxLength(128).HasColumnType("nvarchar(128)");
            b.Property<Guid?>("ResponsePaymentId").HasColumnType("uniqueidentifier");
            b.Property<string>("Status").IsRequired().HasMaxLength(50).HasColumnType("nvarchar(50)");
            b.Property<int?>("StatusCode").HasColumnType("int");
            b.Property<DateTimeOffset>("UpdatedAt").HasColumnType("datetimeoffset");
            b.HasKey("Id");
            b.HasIndex("Key").IsUnique();
            b.ToTable("IdempotencyRecords");
        });

        modelBuilder.Entity("Billing.Domain.Invoice", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedOnAdd().HasColumnType("uniqueidentifier");
            b.Property<decimal>("AmountDue").HasPrecision(18, 2).HasColumnType("decimal(18,2)");
            b.Property<decimal>("AmountPaid").HasPrecision(18, 2).HasColumnType("decimal(18,2)");
            b.Property<DateOnly>("BillingPeriodEnd").HasColumnType("date");
            b.Property<DateOnly>("BillingPeriodStart").HasColumnType("date");
            b.Property<DateTimeOffset>("CreatedAt").HasColumnType("datetimeoffset");
            b.Property<Guid>("CustomerId").HasColumnType("uniqueidentifier");
            b.Property<DateOnly>("DueDate").HasColumnType("date");
            b.Property<string>("Status").IsRequired().HasMaxLength(50).HasColumnType("nvarchar(50)");
            b.Property<Guid>("SubscriptionId").HasColumnType("uniqueidentifier");
            b.HasKey("Id");
            b.HasIndex("CustomerId");
            b.HasIndex("Status");
            b.HasIndex("SubscriptionId", "BillingPeriodStart", "BillingPeriodEnd").IsUnique();
            b.ToTable("Invoices");
        });

        modelBuilder.Entity("Billing.Domain.InvoiceLine", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedOnAdd().HasColumnType("uniqueidentifier");
            b.Property<decimal>("Amount").HasPrecision(18, 2).HasColumnType("decimal(18,2)");
            b.Property<string>("Description").IsRequired().HasMaxLength(500).HasColumnType("nvarchar(500)");
            b.Property<Guid>("InvoiceId").HasColumnType("uniqueidentifier");
            b.Property<decimal>("Quantity").HasPrecision(18, 4).HasColumnType("decimal(18,4)");
            b.Property<decimal>("UnitPrice").HasPrecision(18, 4).HasColumnType("decimal(18,4)");
            b.HasKey("Id");
            b.HasIndex("InvoiceId");
            b.ToTable("InvoiceLines");
        });

        modelBuilder.Entity("Billing.Domain.Payment", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedOnAdd().HasColumnType("uniqueidentifier");
            b.Property<decimal>("Amount").HasPrecision(18, 2).HasColumnType("decimal(18,2)");
            b.Property<DateTimeOffset>("CreatedAt").HasColumnType("datetimeoffset");
            b.Property<string>("IdempotencyKey").IsRequired().HasMaxLength(200).HasColumnType("nvarchar(200)");
            b.Property<Guid>("InvoiceId").HasColumnType("uniqueidentifier");
            b.Property<string>("Provider").IsRequired().HasMaxLength(100).HasColumnType("nvarchar(100)");
            b.Property<string>("Status").IsRequired().HasMaxLength(50).HasColumnType("nvarchar(50)");
            b.HasKey("Id");
            b.HasIndex("IdempotencyKey").IsUnique();
            b.HasIndex("InvoiceId");
            b.ToTable("Payments");
        });

        modelBuilder.Entity("Billing.Domain.PaymentAttempt", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedOnAdd().HasColumnType("uniqueidentifier");
            b.Property<int>("AttemptNumber").HasColumnType("int");
            b.Property<DateTimeOffset>("CreatedAt").HasColumnType("datetimeoffset");
            b.Property<string>("FailureReason").HasMaxLength(500).HasColumnType("nvarchar(500)");
            b.Property<Guid>("PaymentId").HasColumnType("uniqueidentifier");
            b.Property<string>("Status").IsRequired().HasMaxLength(50).HasColumnType("nvarchar(50)");
            b.HasKey("Id");
            b.HasIndex("PaymentId", "AttemptNumber").IsUnique();
            b.ToTable("PaymentAttempts");
        });

        modelBuilder.Entity("Billing.Domain.PricePlan", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedOnAdd().HasColumnType("uniqueidentifier");
            b.Property<bool>("Active").HasColumnType("bit");
            b.Property<decimal>("Amount").HasPrecision(18, 2).HasColumnType("decimal(18,2)");
            b.Property<string>("BillingInterval").IsRequired().HasMaxLength(20).HasColumnType("nvarchar(20)");
            b.Property<string>("BillingType").IsRequired().HasMaxLength(50).HasColumnType("nvarchar(50)");
            b.Property<string>("Currency").IsRequired().HasMaxLength(3).HasColumnType("nvarchar(3)");
            b.Property<Guid>("ProductId").HasColumnType("uniqueidentifier");
            b.Property<string>("UsageUnit").HasMaxLength(100).HasColumnType("nvarchar(100)");
            b.HasKey("Id");
            b.HasIndex("ProductId");
            b.ToTable("PricePlans");
        });

        modelBuilder.Entity("Billing.Domain.Product", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedOnAdd().HasColumnType("uniqueidentifier");
            b.Property<bool>("Active").HasColumnType("bit");
            b.Property<string>("Description").IsRequired().HasMaxLength(500).HasColumnType("nvarchar(500)");
            b.Property<string>("Name").IsRequired().HasMaxLength(200).HasColumnType("nvarchar(200)");
            b.HasKey("Id");
            b.ToTable("Products");
        });

        modelBuilder.Entity("Billing.Domain.Subscription", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedOnAdd().HasColumnType("uniqueidentifier");
            b.Property<bool>("CancelAtPeriodEnd").HasColumnType("bit");
            b.Property<DateOnly>("CurrentPeriodEnd").HasColumnType("date");
            b.Property<DateOnly>("CurrentPeriodStart").HasColumnType("date");
            b.Property<Guid>("CustomerId").HasColumnType("uniqueidentifier");
            b.Property<Guid>("PricePlanId").HasColumnType("uniqueidentifier");
            b.Property<DateOnly>("StartDate").HasColumnType("date");
            b.Property<string>("Status").IsRequired().HasMaxLength(50).HasColumnType("nvarchar(50)");
            b.HasKey("Id");
            b.HasIndex("CustomerId");
            b.HasIndex("PricePlanId");
            b.ToTable("Subscriptions");
        });

        modelBuilder.Entity("Billing.Domain.Invoice", b =>
        {
            b.HasOne("Billing.Domain.Customer", "Customer").WithMany().HasForeignKey("CustomerId").OnDelete(DeleteBehavior.Cascade).IsRequired();
            b.HasOne("Billing.Domain.Subscription", "Subscription").WithMany().HasForeignKey("SubscriptionId").OnDelete(DeleteBehavior.NoAction).IsRequired();
            b.Navigation("Customer");
            b.Navigation("Subscription");
        });

        modelBuilder.Entity("Billing.Domain.InvoiceLine", b =>
        {
            b.HasOne("Billing.Domain.Invoice", "Invoice").WithMany("Lines").HasForeignKey("InvoiceId").OnDelete(DeleteBehavior.Cascade).IsRequired();
            b.Navigation("Invoice");
        });

        modelBuilder.Entity("Billing.Domain.Payment", b =>
        {
            b.HasOne("Billing.Domain.Invoice", "Invoice").WithMany().HasForeignKey("InvoiceId").OnDelete(DeleteBehavior.Cascade).IsRequired();
            b.Navigation("Invoice");
        });

        modelBuilder.Entity("Billing.Domain.PaymentAttempt", b =>
        {
            b.HasOne("Billing.Domain.Payment", "Payment").WithMany("Attempts").HasForeignKey("PaymentId").OnDelete(DeleteBehavior.Cascade).IsRequired();
            b.Navigation("Payment");
        });

        modelBuilder.Entity("Billing.Domain.PricePlan", b =>
        {
            b.HasOne("Billing.Domain.Product", "Product").WithMany().HasForeignKey("ProductId").OnDelete(DeleteBehavior.Cascade).IsRequired();
            b.Navigation("Product");
        });

        modelBuilder.Entity("Billing.Domain.Subscription", b =>
        {
            b.HasOne("Billing.Domain.Customer", "Customer").WithMany().HasForeignKey("CustomerId").OnDelete(DeleteBehavior.Cascade).IsRequired();
            b.HasOne("Billing.Domain.PricePlan", "PricePlan").WithMany().HasForeignKey("PricePlanId").OnDelete(DeleteBehavior.Cascade).IsRequired();
            b.Navigation("Customer");
            b.Navigation("PricePlan");
        });

        modelBuilder.Entity("Billing.Domain.Invoice", b => b.Navigation("Lines"));
        modelBuilder.Entity("Billing.Domain.Payment", b => b.Navigation("Attempts"));
#pragma warning restore 612, 618
    }
}
