using System.Net;
using System.Net.Http.Json;
using Billing.Api.Data;
using Billing.Api.Domain;
using Billing.Api.Features.Common;
using Billing.Api.Features.Invoices;
using Billing.Api.Features.Payments;
using Billing.Api.Features.Subscriptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Billing.Api.Tests;

public sealed class PaymentEndpointTests : IClassFixture<BillingApiFactory>
{
    private readonly BillingApiFactory _factory;

    public PaymentEndpointTests(BillingApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PayInvoice_ReturnsBadRequest_WhenIdempotencyKeyIsMissing()
    {
        var client = _factory.CreateClient();
        var invoice = await CreateInvoiceAsync(client);

        var httpResponse = await PayInvoiceAsync(client, invoice.Id, idempotencyKey: null, new PayInvoiceRequest());

        Assert.Equal(HttpStatusCode.BadRequest, httpResponse.StatusCode);

        var error = await httpResponse.Content.ReadFromJsonAsync<ApiError>();
        Assert.NotNull(error);
        Assert.Equal("idempotency_key_required", error.Code);
    }

    [Fact]
    public async Task PayInvoice_MarksInvoicePaid_AndCreatesPaymentAttempt()
    {
        var client = _factory.CreateClient();
        var invoice = await CreateInvoiceAsync(client);

        var httpResponse = await PayInvoiceAsync(client, invoice.Id, "pay-success-1", new PayInvoiceRequest());

        Assert.Equal(HttpStatusCode.Created, httpResponse.StatusCode);

        var response = await httpResponse.Content.ReadFromJsonAsync<PaymentResponse>();
        Assert.NotNull(response);
        Assert.Equal(invoice.Id, response.InvoiceId);
        Assert.Equal(49m, response.Amount);
        Assert.Equal(PaymentStatuses.Succeeded, response.Status);
        Assert.Equal("test_gateway", response.Provider);
        Assert.Equal("pay-success-1", response.IdempotencyKey);
        Assert.Equal($"/payments/{response.Id}", httpResponse.Headers.Location?.OriginalString);

        var attempt = Assert.Single(response.Attempts);
        Assert.Equal(1, attempt.AttemptNumber);
        Assert.Equal(PaymentAttemptStatuses.Succeeded, attempt.Status);
        Assert.Null(attempt.FailureReason);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BillingDbContext>();
        var savedInvoice = await db.Invoices.SingleAsync(item => item.Id == invoice.Id);
        Assert.Equal(InvoiceStatuses.Paid, savedInvoice.Status);
        Assert.Equal(49m, savedInvoice.AmountPaid);

        var idempotencyRecord = await db.IdempotencyRecords.SingleAsync(item => item.Key == "pay-success-1");
        Assert.Equal(IdempotencyRecordStatuses.Completed, idempotencyRecord.Status);
        Assert.Equal(response.Id, idempotencyRecord.ResponsePaymentId);
    }

    [Fact]
    public async Task PayInvoice_ReturnsExistingPayment_ForDuplicateIdempotencyKey()
    {
        var client = _factory.CreateClient();
        var invoice = await CreateInvoiceAsync(client);
        var request = new PayInvoiceRequest();

        var firstHttpResponse = await PayInvoiceAsync(client, invoice.Id, "pay-duplicate-1", request);
        var secondHttpResponse = await PayInvoiceAsync(client, invoice.Id, "pay-duplicate-1", request);

        Assert.Equal(HttpStatusCode.Created, firstHttpResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondHttpResponse.StatusCode);

        var first = await firstHttpResponse.Content.ReadFromJsonAsync<PaymentResponse>();
        var second = await secondHttpResponse.Content.ReadFromJsonAsync<PaymentResponse>();
        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal(first.Id, second.Id);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BillingDbContext>();
        var paymentCount = await db.Payments.CountAsync(payment => payment.InvoiceId == invoice.Id);
        var attemptCount = await db.PaymentAttempts.CountAsync(attempt => attempt.PaymentId == first.Id);
        Assert.Equal(1, paymentCount);
        Assert.Equal(1, attemptCount);
    }

    [Fact]
    public async Task PayInvoice_ReturnsConflict_WhenIdempotencyKeyIsReusedForDifferentRequest()
    {
        var client = _factory.CreateClient();
        var invoice = await CreateInvoiceAsync(client);

        var firstHttpResponse = await PayInvoiceAsync(
            client,
            invoice.Id,
            "pay-conflict-1",
            new PayInvoiceRequest(Provider: "gateway_a"));

        var secondHttpResponse = await PayInvoiceAsync(
            client,
            invoice.Id,
            "pay-conflict-1",
            new PayInvoiceRequest(Provider: "gateway_b"));

        Assert.Equal(HttpStatusCode.Created, firstHttpResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, secondHttpResponse.StatusCode);

        var error = await secondHttpResponse.Content.ReadFromJsonAsync<ApiError>();
        Assert.NotNull(error);
        Assert.Equal("idempotency_key_reused", error.Code);
    }

    [Fact]
    public async Task PayInvoice_CreatesSinglePayment_ForConcurrentDuplicateRequests()
    {
        var client = _factory.CreateClient();
        var invoice = await CreateInvoiceAsync(client);
        var request = new PayInvoiceRequest();

        var httpResponses = await Task.WhenAll(
            Enumerable.Range(0, 8)
                .Select(_ => PayInvoiceAsync(client, invoice.Id, "pay-concurrent-1", request)));

        Assert.All(httpResponses, response =>
        {
            Assert.True(
                response.StatusCode is HttpStatusCode.Created or HttpStatusCode.OK,
                $"Expected Created or OK, got {response.StatusCode}.");
        });

        var payments = await Task.WhenAll(
            httpResponses.Select(async response =>
            {
                var payment = await response.Content.ReadFromJsonAsync<PaymentResponse>();
                Assert.NotNull(payment);
                return payment;
            }));

        var paymentId = Assert.Single(payments.Select(payment => payment.Id).Distinct());
        Assert.NotEqual(Guid.Empty, paymentId);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BillingDbContext>();
        var paymentCount = await db.Payments.CountAsync(payment => payment.InvoiceId == invoice.Id);
        var attemptCount = await db.PaymentAttempts.CountAsync(attempt => attempt.PaymentId == paymentId);
        Assert.Equal(1, paymentCount);
        Assert.Equal(1, attemptCount);
    }

    [Fact]
    public async Task PayInvoice_CreatesSingleSuccessfulPayment_ForConcurrentDifferentIdempotencyKeys()
    {
        var client = _factory.CreateClient();
        var invoice = await CreateInvoiceAsync(client);
        var request = new PayInvoiceRequest();

        var httpResponses = await Task.WhenAll(
            Enumerable.Range(0, 8)
                .Select(index => PayInvoiceAsync(client, invoice.Id, $"pay-race-{index}", request)));

        var createdResponses = httpResponses
            .Where(response => response.StatusCode == HttpStatusCode.Created)
            .ToArray();
        var conflictResponses = httpResponses
            .Where(response => response.StatusCode == HttpStatusCode.Conflict)
            .ToArray();

        Assert.Single(createdResponses);
        Assert.Equal(7, conflictResponses.Length);

        var errors = await Task.WhenAll(
            conflictResponses.Select(response => response.Content.ReadFromJsonAsync<ApiError>()));
        Assert.All(errors, error =>
        {
            Assert.NotNull(error);
            Assert.Equal("invoice_already_paid", error.Code);
        });

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BillingDbContext>();
        var successfulPaymentCount = await db.Payments.CountAsync(
            payment => payment.InvoiceId == invoice.Id
                && payment.Status == PaymentStatuses.Succeeded);
        var savedInvoice = await db.Invoices.SingleAsync(item => item.Id == invoice.Id);

        Assert.Equal(1, successfulPaymentCount);
        Assert.Equal(InvoiceStatuses.Paid, savedInvoice.Status);
        Assert.Equal(49m, savedInvoice.AmountPaid);
    }

    [Fact]
    public async Task PayInvoice_ReturnsSameFailure_ForDuplicateFailedGatewayRequest()
    {
        var client = _factory.CreateClient();
        var invoice = await CreateInvoiceAsync(client);
        var request = new PayInvoiceRequest(SimulateFailure: true);

        var firstHttpResponse = await PayInvoiceAsync(client, invoice.Id, "pay-failure-1", request);
        var secondHttpResponse = await PayInvoiceAsync(client, invoice.Id, "pay-failure-1", request);

        Assert.Equal(HttpStatusCode.BadGateway, firstHttpResponse.StatusCode);
        Assert.Equal(HttpStatusCode.BadGateway, secondHttpResponse.StatusCode);

        var firstError = await firstHttpResponse.Content.ReadFromJsonAsync<ApiError>();
        var secondError = await secondHttpResponse.Content.ReadFromJsonAsync<ApiError>();
        Assert.NotNull(firstError);
        Assert.NotNull(secondError);
        Assert.Equal("payment_gateway_failed", firstError.Code);
        Assert.Equal(firstError, secondError);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BillingDbContext>();
        var savedInvoice = await db.Invoices.SingleAsync(item => item.Id == invoice.Id);
        Assert.Equal(InvoiceStatuses.Open, savedInvoice.Status);
        Assert.Equal(0m, savedInvoice.AmountPaid);

        var payment = await db.Payments
            .Include(item => item.Attempts)
            .SingleAsync(item => item.InvoiceId == invoice.Id);
        Assert.Equal(PaymentStatuses.Failed, payment.Status);
        var attempt = Assert.Single(payment.Attempts);
        Assert.Equal(PaymentAttemptStatuses.Failed, attempt.Status);

        var idempotencyRecord = await db.IdempotencyRecords.SingleAsync(item => item.Key == "pay-failure-1");
        Assert.Equal(IdempotencyRecordStatuses.Failed, idempotencyRecord.Status);
        Assert.Equal(payment.Id, idempotencyRecord.ResponsePaymentId);
    }

    private static async Task<InvoiceResponse> CreateInvoiceAsync(HttpClient client)
    {
        var subscriptionRequest = new CreateSubscriptionRequest(
            BillingSeedData.SampleCustomerId,
            BillingSeedData.ProPlanId,
            new DateOnly(2026, 5, 5));

        var subscriptionHttpResponse = await client.PostAsJsonAsync("/subscriptions", subscriptionRequest);
        subscriptionHttpResponse.EnsureSuccessStatusCode();

        var subscription = await subscriptionHttpResponse.Content.ReadFromJsonAsync<SubscriptionResponse>();
        Assert.NotNull(subscription);

        var invoiceHttpResponse = await client.PostAsJsonAsync(
            "/invoices/generate",
            new GenerateInvoiceRequest(subscription.Id));
        invoiceHttpResponse.EnsureSuccessStatusCode();

        var invoice = await invoiceHttpResponse.Content.ReadFromJsonAsync<InvoiceResponse>();
        Assert.NotNull(invoice);
        return invoice;
    }

    private static Task<HttpResponseMessage> PayInvoiceAsync(
        HttpClient client,
        Guid invoiceId,
        string? idempotencyKey,
        PayInvoiceRequest request)
    {
        var message = new HttpRequestMessage(HttpMethod.Post, $"/invoices/{invoiceId}/pay")
        {
            Content = JsonContent.Create(request)
        };

        if (idempotencyKey is not null)
        {
            message.Headers.Add("Idempotency-Key", idempotencyKey);
        }

        return client.SendAsync(message);
    }
}
