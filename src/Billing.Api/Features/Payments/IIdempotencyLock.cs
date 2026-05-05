namespace Billing.Api.Features.Payments;

public interface IIdempotencyLock
{
    Task<IDisposable> AcquireAsync(string key, CancellationToken cancellationToken);
}
