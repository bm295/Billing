namespace Billing.Application.Subscriptions;

public sealed class ProrationCalculator
{
    public ProrationResult Calculate(
        decimal currentPlanAmount,
        decimal newPlanAmount,
        DateOnly periodStart,
        DateOnly periodEnd,
        DateOnly changeDate)
    {
        if (periodEnd <= periodStart)
        {
            throw new ArgumentException("Period end must be after period start.", nameof(periodEnd));
        }

        var totalDays = periodEnd.DayNumber - periodStart.DayNumber;
        var remainingDays = CalculateRemainingDays(periodStart, periodEnd, changeDate);

        var unusedCredit = CalculateProratedAmount(currentPlanAmount, remainingDays, totalDays);
        var remainingPlanCost = CalculateProratedAmount(newPlanAmount, remainingDays, totalDays);

        return new ProrationResult(
            unusedCredit,
            remainingPlanCost,
            remainingPlanCost - unusedCredit);
    }

    private static int CalculateRemainingDays(DateOnly periodStart, DateOnly periodEnd, DateOnly changeDate)
    {
        if (changeDate <= periodStart)
        {
            return periodEnd.DayNumber - periodStart.DayNumber;
        }

        if (changeDate >= periodEnd)
        {
            return 0;
        }

        return periodEnd.DayNumber - changeDate.DayNumber;
    }

    private static decimal CalculateProratedAmount(decimal amount, int remainingDays, int totalDays)
    {
        var proratedAmount = amount * remainingDays / totalDays;
        return decimal.Round(proratedAmount, 2, MidpointRounding.AwayFromZero);
    }
}
