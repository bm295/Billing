using Billing.Infrastructure.Persistence;
using Billing.Infrastructure.Payments;
using Billing.Application.Invoices.Services;
using Billing.Application.Payments;
using Billing.Domain.Concurrency;
using Billing.Application.Subscriptions;
using Billing.Api.Endpoints;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IKeyedLock, IdempotencyLock>();
builder.Services.AddScoped<IInvoiceService, InvoiceService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddDbContext<BillingDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("BillingDb")
        ?? throw new InvalidOperationException("Connection string 'BillingDb' is not configured.");

    options.UseSqlServer(connectionString);
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BillingDbContext>();
    await BillingSeedData.SeedAsync(db);
}

app.MapGet("/", () => Results.Ok(new { service = "Billing API" }));
app.MapSubscriptionEndpoints();
app.MapInvoiceEndpoints();

app.Run();

public partial class Program;
