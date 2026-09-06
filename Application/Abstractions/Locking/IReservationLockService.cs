namespace Application.Abstractions.Locking;

public interface IReservationLockService
{
    Task<bool> AcquireAsync(Guid batchId, string token, CancellationToken cancellationToken = default);

    Task ReleaseAsync(Guid batchId, string token, CancellationToken cancellationToken = default);
}