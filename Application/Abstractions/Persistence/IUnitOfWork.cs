namespace Application.Abstractions.Persistence;

public interface IUnitOfWork
{
    Task<ITransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}

public interface ITransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken = default);

    Task RollbackAsync(CancellationToken cancellationToken = default);
}