using Billing.Api.Domain;

namespace Billing.Api.Services;

public sealed record PaymentResult(
    PaymentOutcome Outcome,
    int StatusCode,
    Payment? Payment,
    string? ErrorCode = null,
    string? ErrorMessage = null)
{
    public static PaymentResult Created(Payment payment)
    {
        return new PaymentResult(PaymentOutcome.Created, StatusCodes.Status201Created, payment);
    }

    public static PaymentResult Existing(Payment payment)
    {
        return new PaymentResult(PaymentOutcome.Existing, StatusCodes.Status200OK, payment);
    }

    public static PaymentResult Failed(int statusCode, string errorCode, string errorMessage)
    {
        return new PaymentResult(PaymentOutcome.Failed, statusCode, null, errorCode, errorMessage);
    }

    public static PaymentResult FailedWithPayment(
        int statusCode,
        Payment payment,
        string errorCode,
        string errorMessage)
    {
        return new PaymentResult(PaymentOutcome.Failed, statusCode, payment, errorCode, errorMessage);
    }
}
