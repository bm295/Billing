namespace Billing.Application.Subscriptions;

public sealed record ProrationResult(
    decimal UnusedCredit,
    decimal RemainingPlanCost,
    decimal NetAmountDue);
