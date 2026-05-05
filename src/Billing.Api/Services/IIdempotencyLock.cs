namespace Billing.Api.Services;

public interface IIdempotencyLock
{
    Task<IDisposable> AcquireAsync(string key, CancellationToken cancellationToken);
}
