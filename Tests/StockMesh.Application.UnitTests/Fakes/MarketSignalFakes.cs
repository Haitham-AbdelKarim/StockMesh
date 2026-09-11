using Application.Abstractions.Repositories;
using Domain.Entities;
using Domain.Enums;

namespace StockMesh.Application.UnitTests.Fakes;

internal sealed class FakeDailyMarketSignalRepository : IDailyMarketSignalRepository
{
    private readonly List<DailyMarketSignal> _signals = new();

    public FakeDailyMarketSignalRepository(params DailyMarketSignal[] signals)
    {
        _signals.AddRange(signals);
    }

    public IReadOnlyList<DailyMarketSignal> All => _signals;

    public Task<DailyMarketSignal?> GetByDateAsync(
        Guid productId,
        VerticalCategory verticalCategory,
        DateTime date,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(
            _signals.FirstOrDefault(s =>
                s.ProductId == productId
                && s.VerticalCategory == verticalCategory
                && s.Date == date));
    }

    public Task<IReadOnlyList<DailyMarketSignal>> GetHistoryAsync(
        Guid productId,
        VerticalCategory verticalCategory,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<DailyMarketSignal>>(
            _signals
                .Where(s => s.ProductId == productId
                    && s.VerticalCategory == verticalCategory
                    && s.Date >= from
                    && s.Date < to)
                .OrderBy(s => s.Date)
                .ToList());
    }

    public Task AddAsync(
        DailyMarketSignal signal,
        CancellationToken cancellationToken = default)
    {
        _signals.Add(signal);

        return Task.CompletedTask;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(0);
    }
}