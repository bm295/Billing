using Billing.Api.Domain;

namespace Billing.Api.Features.Payments;

public sealed record PaymentAttemptResponse(
    Guid Id,
    int AttemptNumber,
    string Status,
    string? FailureReason,
    DateTimeOffset CreatedAt)
{
    public static PaymentAttemptResponse FromEntity(PaymentAttempt attempt)
    {
        return new PaymentAttemptResponse(
            attempt.Id,
            attempt.AttemptNumber,
            attempt.Status,
            attempt.FailureReason,
            attempt.CreatedAt);
    }
}
