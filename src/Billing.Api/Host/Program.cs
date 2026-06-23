using Billing.Api.Options;
using Billing.Application.Customers;
using Billing.Infrastructure.Persistence;
using Billing.Infrastructure.Payments;
using Billing.Application.Invoices.Services;
using Billing.Application.Payments;
using Billing.Domain.Concurrency;
using Billing.Application.Subscriptions;
using Billing.Api.Endpoints;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IKeyedLock, IdempotencyLock>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<IInvoiceService, InvoiceService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddSingleton<IValidateOptions<BillingDatabaseOptions>, BillingDatabaseOptionsValidator>();
builder.Services.AddOptions<BillingDatabaseOptions>()
    .Configure(options =>
    {
        options.ConnectionString = builder.Configuration.GetConnectionString(BillingDatabaseOptions.ConnectionStringName);
    })
    .ValidateOnStart();
builder.Services.AddDbContext<BillingDbContext>((serviceProvider, options) =>
{
    var databaseOptions = serviceProvider.GetRequiredService<IOptions<BillingDatabaseOptions>>().Value;
    var connectionString = databaseOptions.ConnectionString
        ?? throw new InvalidOperationException(
            $"Connection string '{BillingDatabaseOptions.ConnectionStringName}' is not configured.");

    options.UseSqlServer(connectionString);
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BillingDbContext>();
    await BillingSeedData.SeedAsync(db);
}

app.MapGet("/", () => Results.Ok(new { service = "Billing API" }));
app.MapCustomerEndpoints();
app.MapSubscriptionEndpoints();
app.MapInvoiceEndpoints();

app.Run();

public partial class Program;
