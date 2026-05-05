namespace Billing.Domain.Concurrency;

public interface IKeyedLock
{
    Task<IDisposable> AcquireAsync(string key, CancellationToken cancellationToken);
}
