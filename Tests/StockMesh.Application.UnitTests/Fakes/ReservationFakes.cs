using Application.Abstractions.Locking;
using Application.Abstractions.Persistence;
using Application.Abstractions.Repositories;
using Application.Abstractions.Services;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace StockMesh.Application.UnitTests.Fakes;

internal sealed class FakeDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow { get; set; } = new DateTime(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);
}

internal sealed class FakeTransaction : ITransaction
{
    public bool Committed { get; private set; }

    public bool RolledBack { get; private set; }

    public Task CommitAsync(CancellationToken cancellationToken = default)
    {
        Committed = true;

        return Task.CompletedTask;
    }

    public Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        RolledBack = true;

        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}

internal sealed class FakeUnitOfWork : IUnitOfWork
{
    private readonly List<FakeTransaction> _transactions = new();

    public IReadOnlyList<FakeTransaction> Transactions => _transactions;

    public int BeginCount => _transactions.Count;

    public Task<ITransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        var transaction = new FakeTransaction();
        _transactions.Add(transaction);

        return Task.FromResult<ITransaction>(transaction);
    }
}

internal sealed class FakeReservationLockService : IReservationLockService
{
    private readonly SemaphoreSlim? _gate;
    private readonly bool _acquireResult;

    public FakeReservationLockService(bool acquireResult = true, bool serialize = false)
    {
        _acquireResult = acquireResult;
        _gate = serialize ? new SemaphoreSlim(1, 1) : null;
    }

    public int AcquireCount { get; private set; }

    public int ReleaseCount { get; private set; }

    public async Task<bool> AcquireAsync(
        Guid batchId,
        string token,
        CancellationToken cancellationToken = default)
    {
        AcquireCount++;

        if (_gate is not null)
        {
            await _gate.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);
        }

        return _acquireResult;
    }

    public Task ReleaseAsync(
        Guid batchId,
        string token,
        CancellationToken cancellationToken = default)
    {
        ReleaseCount++;

        _gate?.Release();

        return Task.CompletedTask;
    }
}

internal sealed class FakeStockReservationRepository : IStockReservationRepository
{
    private readonly List<StockReservation> _reservations;

    public FakeStockReservationRepository(params StockReservation[] reservations)
    {
        _reservations = reservations.ToList();
    }

    public Exception? SaveException { get; init; }

    public IReadOnlyList<StockReservation> All => _reservations;

    public Task<StockReservation?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_reservations.FirstOrDefault(r => r.Id == id));
    }

    public Task<IReadOnlyList<StockReservation>> GetPendingForBatchAsync(
        Guid batchId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<StockReservation>>(
            _reservations
                .Where(r => r.BatchId == batchId && r.Status == ReservationStatus.Pending)
                .ToList());
    }

    public Task<IReadOnlyList<StockReservation>> GetByStatusAsync(
        ReservationStatus status,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<StockReservation>>(
            _reservations.Where(r => r.Status == status).ToList());
    }

    public Task<IReadOnlyList<StockReservation>> GetExpiredPendingAsync(
        DateTime nowUtc,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<StockReservation>>(
            _reservations
                .Where(r => r.Status == ReservationStatus.Pending && r.HoldExpiresAt <= nowUtc)
                .ToList());
    }

    public Task<IReadOnlyList<StockReservation>> GetForStoreByDateRangeAsync(
        Guid storeId,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<StockReservation>>(
            _reservations
                .Where(r => (r.RequestingStoreId == storeId || r.OwningStoreId == storeId)
                    && r.CreatedAt >= from
                    && r.CreatedAt < to)
                .ToList());
    }

    public Task<(IReadOnlyList<StockReservation> Items, int TotalCount)> GetForStoreAsync(
        Guid storeId,
        bool? incoming,
        ReservationStatus? status,
        DateTime? from,
        DateTime? to,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _reservations.AsEnumerable();

        if (incoming is true)
        {
            query = query.Where(r => r.OwningStoreId == storeId);
        }
        else if (incoming is false)
        {
            query = query.Where(r => r.RequestingStoreId == storeId);
        }
        else
        {
            query = query.Where(r => r.RequestingStoreId == storeId || r.OwningStoreId == storeId);
        }

        if (status is { } reservationStatus)
        {
            query = query.Where(r => r.Status == reservationStatus);
        }

        if (from is { } fromDate)
        {
            query = query.Where(r => r.CreatedAt >= fromDate);
        }

        if (to is { } toDate)
        {
            query = query.Where(r => r.CreatedAt <= toDate);
        }

        var all = query
            .OrderByDescending(r => r.CreatedAt)
            .ToList();

        var items = all
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return Task.FromResult(((IReadOnlyList<StockReservation>)items, all.Count));
    }

    public Task<Guid> AddAsync(
        StockReservation reservation,
        CancellationToken cancellationToken = default)
    {
        _reservations.Add(reservation);

        return Task.FromResult(reservation.Id);
    }

    public void Update(StockReservation reservation)
    {
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (SaveException is not null)
        {
            throw SaveException;
        }

        await Task.Yield();

        return 0;
    }
}

internal sealed class FakeAuditLogRepository : IAuditLogRepository
{
    private readonly List<AuditLog> _entries = new();

    public IReadOnlyList<AuditLog> Entries => _entries;

    public Task<Guid> AddAsync(AuditLog entry, CancellationToken cancellationToken = default)
    {
        _entries.Add(entry);

        return Task.FromResult(entry.Id);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(0);
    }
}

internal sealed class FakeMediator : IMediator
{
    public List<INotification> Published { get; } = new();

    public Task<TResponse> Send<TResponse>(
        IRequest<TResponse> request,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }

    public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
        where TRequest : IRequest
    {
        throw new NotSupportedException();
    }

    public Task<object?> Send(object request, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }

    public IAsyncEnumerable<TResponse> CreateStream<TResponse>(
        IStreamRequest<TResponse> request,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }

    public IAsyncEnumerable<object?> CreateStream(
        object request,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }

    public Task Publish(
        object notification,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }

    public Task Publish<TNotification>(
        TNotification notification,
        CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        Published.Add(notification);

        return Task.CompletedTask;
    }
}