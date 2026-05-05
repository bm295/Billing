using Billing.Api.Contracts;
using Billing.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Billing.Api.Endpoints;

public static class InvoiceEndpoints
{
    public static IEndpointRouteBuilder MapInvoiceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/invoices");

        group.MapPost("/generate", GenerateInvoiceAsync)
            .WithName("GenerateInvoice");

        group.MapPost("/{invoiceId:guid}/pay", PayInvoiceAsync)
            .WithName("PayInvoice");

        return app;
    }

    private static async Task<IResult> GenerateInvoiceAsync(
        GenerateInvoiceRequest request,
        IInvoiceService invoiceService,
        CancellationToken cancellationToken)
    {
        var result = await invoiceService.GenerateRecurringInvoiceAsync(
            request.SubscriptionId,
            cancellationToken);

        if (result.Invoice is null)
        {
            return Results.BadRequest(new ApiError(result.ErrorCode!, result.ErrorMessage!));
        }

        var response = InvoiceResponse.FromEntity(result.Invoice);
        return result.Outcome switch
        {
            InvoiceGenerationOutcome.Created => Results.Created($"/invoices/{result.Invoice.Id}", response),
            InvoiceGenerationOutcome.Existing => Results.Ok(response),
            _ => Results.BadRequest(new ApiError(result.ErrorCode!, result.ErrorMessage!))
        };
    }

    private static async Task<IResult> PayInvoiceAsync(
        Guid invoiceId,
        PayInvoiceRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        IPaymentService paymentService,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return Results.BadRequest(new ApiError(
                "idempotency_key_required",
                "Payment requests require an Idempotency-Key header."));
        }

        var result = await paymentService.PayInvoiceAsync(
            invoiceId,
            request,
            idempotencyKey.Trim(),
            cancellationToken);

        if (result.Payment is not null)
        {
            var response = PaymentResponse.FromEntity(result.Payment);
            return result.Outcome switch
            {
                PaymentOutcome.Created => Results.Created($"/payments/{result.Payment.Id}", response),
                PaymentOutcome.Existing => Results.Ok(response),
                _ => Results.Json(
                    new ApiError(result.ErrorCode!, result.ErrorMessage!),
                    statusCode: result.StatusCode)
            };
        }

        return Results.Json(
            new ApiError(result.ErrorCode!, result.ErrorMessage!),
            statusCode: result.StatusCode);
    }
}
