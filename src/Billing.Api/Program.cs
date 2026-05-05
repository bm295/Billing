using Billing.Api.Data;
using Billing.Api.Features.Invoices;
using Billing.Api.Features.Payments;
using Billing.Api.Features.Subscriptions;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IInvoiceGenerationLock, InvoiceGenerationLock>();
builder.Services.AddSingleton<IIdempotencyLock, IdempotencyLock>();
builder.Services.AddScoped<IInvoiceService, InvoiceService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddDbContext<BillingDbContext>(options =>
{
    options.UseInMemoryDatabase("Billing");
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
