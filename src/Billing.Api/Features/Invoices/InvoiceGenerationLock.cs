using System.Collections.Concurrent;

namespace Billing.Api.Features.Invoices;

public sealed class InvoiceGenerationLock : IInvoiceGenerationLock
{
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

    public async Task<IDisposable> AcquireAsync(
        Guid subscriptionId,
        DateOnly billingPeriodStart,
        DateOnly billingPeriodEnd,
        CancellationToken cancellationToken)
    {
        var key = $"{subscriptionId:N}:{billingPeriodStart:yyyyMMdd}:{billingPeriodEnd:yyyyMMdd}";
        var semaphore = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));

        await semaphore.WaitAsync(cancellationToken);
        return new Lease(semaphore);
    }

    private sealed class Lease(SemaphoreSlim semaphore) : IDisposable
    {
        public void Dispose()
        {
            semaphore.Release();
        }
    }
}
