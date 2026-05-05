using Billing.Domain.Concurrency;
using System.Security.Cryptography;
using System.Text;
using Billing.Infrastructure.Persistence;
using Billing.Domain;
using Microsoft.EntityFrameworkCore;

namespace Billing.Application.Payments;

public sealed class PaymentService(
    BillingDbContext db,
    TimeProvider timeProvider,
    IKeyedLock keyedLock) : IPaymentService
{
    private const string DefaultProvider = "test_gateway";

    public async Task<PaymentResult> PayInvoiceAsync(
        Guid invoiceId,
        PayInvoiceRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var normalizedRequest = NormalizeRequest(request);
        var requestHash = BuildRequestHash(invoiceId, normalizedRequest);

        using var lease = await keyedLock.AcquireAsync(
            $"idempotency:{idempotencyKey}",
            cancellationToken);

        var existingRecord = await FindIdempotencyRecordAsync(idempotencyKey, cancellationToken);
        if (existingRecord is not null)
        {
            return await BuildExistingResultAsync(existingRecord, requestHash, cancellationToken);
        }

        var now = timeProvider.GetUtcNow();
        var record = new IdempotencyRecord
        {
            Id = Guid.NewGuid(),
            Key = idempotencyKey,
            RequestHash = requestHash,
            Status = IdempotencyRecordStatuses.InProgress,
            CreatedAt = now,
            UpdatedAt = now
        };

        db.IdempotencyRecords.Add(record);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            db.Entry(record).State = EntityState.Detached;

            existingRecord = await FindIdempotencyRecordAsync(idempotencyKey, cancellationToken);
            if (existingRecord is not null)
            {
                return await BuildExistingResultAsync(existingRecord, requestHash, cancellationToken);
            }

            throw;
        }

        return await ExecutePaymentAsync(invoiceId, normalizedRequest, record, cancellationToken);
    }

    private async Task<PaymentResult> ExecutePaymentAsync(
        Guid invoiceId,
        PayInvoiceRequest request,
        IdempotencyRecord record,
        CancellationToken cancellationToken)
    {
        using var invoiceLease = await keyedLock.AcquireAsync(
            $"invoice-payment:{invoiceId:N}",
            cancellationToken);

        var invoice = await db.Invoices.SingleOrDefaultAsync(
            item => item.Id == invoiceId,
            cancellationToken);

        if (invoice is null)
        {
            MarkRecordFailed(
                record,
                404,
                "invoice_not_found",
                "Invoice does not exist.");

            await db.SaveChangesAsync(cancellationToken);
            return PaymentResult.Failed(
                404,
                record.ErrorCode!,
                record.ErrorMessage!);
        }

        if (invoice.Status == InvoiceStatuses.Paid)
        {
            MarkRecordFailed(
                record,
                409,
                "invoice_already_paid",
                "Invoice is already paid.");

            await db.SaveChangesAsync(cancellationToken);
            return PaymentResult.Failed(
                409,
                record.ErrorCode!,
                record.ErrorMessage!);
        }

        if (invoice.Status != InvoiceStatuses.Open)
        {
            MarkRecordFailed(
                record,
                409,
                "invoice_not_payable",
                $"Invoice with status '{invoice.Status}' cannot be paid.");

            await db.SaveChangesAsync(cancellationToken);
            return PaymentResult.Failed(
                409,
                record.ErrorCode!,
                record.ErrorMessage!);
        }

        var amountToPay = invoice.AmountDue - invoice.AmountPaid;
        if (amountToPay <= 0m)
        {
            MarkRecordFailed(
                record,
                409,
                "invoice_balance_not_due",
                "Invoice does not have an amount due.");

            await db.SaveChangesAsync(cancellationToken);
            return PaymentResult.Failed(
                409,
                record.ErrorCode!,
                record.ErrorMessage!);
        }

        var payment = BuildPayment(invoice.Id, amountToPay, request, record.Key);
        db.Payments.Add(payment);

        if (request.SimulateFailure)
        {
            MarkPaymentFailed(payment, record);
            await db.SaveChangesAsync(cancellationToken);

            return PaymentResult.FailedWithPayment(
                502,
                payment,
                record.ErrorCode!,
                record.ErrorMessage!);
        }

        payment.Status = PaymentStatuses.Succeeded;
        payment.Attempts[0].Status = PaymentAttemptStatuses.Succeeded;
        invoice.AmountPaid += amountToPay;
        invoice.Status = InvoiceStatuses.Paid;

        record.Status = IdempotencyRecordStatuses.Completed;
        record.StatusCode = 201;
        record.ResponsePaymentId = payment.Id;
        record.UpdatedAt = timeProvider.GetUtcNow();

        await db.SaveChangesAsync(cancellationToken);
        return PaymentResult.Created(payment);
    }

    private async Task<PaymentResult> BuildExistingResultAsync(
        IdempotencyRecord record,
        string requestHash,
        CancellationToken cancellationToken)
    {
        if (record.RequestHash != requestHash)
        {
            return PaymentResult.Failed(
                409,
                "idempotency_key_reused",
                "Idempotency-Key was already used for a different request.");
        }

        if (record.Status == IdempotencyRecordStatuses.Failed)
        {
            return PaymentResult.Failed(
                record.StatusCode ?? 500,
                record.ErrorCode ?? "payment_failed",
                record.ErrorMessage ?? "Payment failed.");
        }

        if (record.ResponsePaymentId is not null)
        {
            var payment = await FindPaymentAsync(record.ResponsePaymentId.Value, cancellationToken);
            if (payment is not null)
            {
                return PaymentResult.Existing(payment);
            }
        }

        return PaymentResult.Failed(
            409,
            "idempotency_request_in_progress",
            "A payment request with this Idempotency-Key is still in progress.");
    }

    private async Task<IdempotencyRecord?> FindIdempotencyRecordAsync(
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        return await db.IdempotencyRecords
            .SingleOrDefaultAsync(record => record.Key == idempotencyKey, cancellationToken);
    }

    private async Task<Payment?> FindPaymentAsync(Guid paymentId, CancellationToken cancellationToken)
    {
        return await db.Payments
            .Include(payment => payment.Attempts)
            .SingleOrDefaultAsync(payment => payment.Id == paymentId, cancellationToken);
    }

    private Payment BuildPayment(
        Guid invoiceId,
        decimal amount,
        PayInvoiceRequest request,
        string idempotencyKey)
    {
        var now = timeProvider.GetUtcNow();
        return new Payment
        {
            Id = Guid.NewGuid(),
            InvoiceId = invoiceId,
            Amount = amount,
            Status = PaymentStatuses.Failed,
            Provider = request.Provider!,
            IdempotencyKey = idempotencyKey,
            CreatedAt = now,
            Attempts =
            [
                new PaymentAttempt
                {
                    Id = Guid.NewGuid(),
                    AttemptNumber = 1,
                    Status = PaymentAttemptStatuses.Failed,
                    CreatedAt = now
                }
            ]
        };
    }

    private void MarkPaymentFailed(Payment payment, IdempotencyRecord record)
    {
        payment.Status = PaymentStatuses.Failed;
        payment.Attempts[0].Status = PaymentAttemptStatuses.Failed;
        payment.Attempts[0].FailureReason = "Simulated payment gateway failure.";

        record.Status = IdempotencyRecordStatuses.Failed;
        record.StatusCode = 502;
        record.ResponsePaymentId = payment.Id;
        record.ErrorCode = "payment_gateway_failed";
        record.ErrorMessage = "Payment gateway failed to charge the payment method.";
        record.UpdatedAt = timeProvider.GetUtcNow();
    }

    private void MarkRecordFailed(
        IdempotencyRecord record,
        int statusCode,
        string errorCode,
        string errorMessage)
    {
        record.Status = IdempotencyRecordStatuses.Failed;
        record.StatusCode = statusCode;
        record.ErrorCode = errorCode;
        record.ErrorMessage = errorMessage;
        record.UpdatedAt = timeProvider.GetUtcNow();
    }

    private static PayInvoiceRequest NormalizeRequest(PayInvoiceRequest request)
    {
        var provider = string.IsNullOrWhiteSpace(request.Provider)
            ? DefaultProvider
            : request.Provider.Trim();

        return request with { Provider = provider };
    }

    private static string BuildRequestHash(Guid invoiceId, PayInvoiceRequest request)
    {
        var payload = $"POST:/invoices/{invoiceId:N}/pay|provider={request.Provider}|simulateFailure={request.SimulateFailure}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(bytes);
    }
}
