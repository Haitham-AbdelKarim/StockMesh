using Application.Abstractions.Repositories;
using Application.Abstractions.Services;
using Domain.Entities;
using Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Application.Features.MarketSignals.Services;

public sealed class MarketSignalAggregator
{
    private readonly IDateTimeProvider _clock;
    private readonly IStockReservationRepository _stockReservationRepository;
    private readonly IStockMovementRepository _stockMovementRepository;
    private readonly IInventoryBatchRepository _inventoryBatchRepository;
    private readonly IProductRepository _productRepository;
    private readonly IDailyMarketSignalRepository _dailyMarketSignalRepository;
    private readonly ILogger<MarketSignalAggregator> _logger;

    public MarketSignalAggregator(
        IDateTimeProvider clock,
        IStockReservationRepository stockReservationRepository,
        IStockMovementRepository stockMovementRepository,
        IInventoryBatchRepository inventoryBatchRepository,
        IProductRepository productRepository,
        IDailyMarketSignalRepository dailyMarketSignalRepository,
        ILogger<MarketSignalAggregator> logger)
    {
        _clock = clock;
        _stockReservationRepository = stockReservationRepository;
        _stockMovementRepository = stockMovementRepository;
        _inventoryBatchRepository = inventoryBatchRepository;
        _productRepository = productRepository;
        _dailyMarketSignalRepository = dailyMarketSignalRepository;
        _logger = logger;
    }

    public async Task RunAsync(
        DateTime? date = null,
        CancellationToken cancellationToken = default)
    {
        var day = date ?? _clock.UtcNow.Date.AddDays(-1);
        var from = day.Date;
        var to = from.AddDays(1);

        var reservations = await _stockReservationRepository.GetByDateRangeAsync(
            from, to, cancellationToken);
        var transfers = await _stockMovementRepository.GetByTypeInRangeAsync(
            MovementType.NetworkTransferOut, from, to, cancellationToken);

        if (reservations.Count == 0 && transfers.Count == 0)
        {
            return;
        }

        var batchIds = reservations.Select(r => r.BatchId)
            .Concat(transfers.Select(m => m.BatchId))
            .Distinct()
            .ToList();
        var batches = await _inventoryBatchRepository.GetByIdsAsync(batchIds, cancellationToken);
        var batchesById = batches.ToDictionary(b => b.Id);

        var productIds = batches.Select(b => b.ProductId).Distinct().ToList();
        var products = await _productRepository.GetByIdsAsync(productIds, cancellationToken);
        var verticalByProductId = products.ToDictionary(p => p.Id, p => p.VerticalCategory);

        var reservationGroups = reservations
            .Where(r => batchesById.TryGetValue(r.BatchId, out var batch)
                && verticalByProductId.TryGetValue(batch.ProductId, out _))
            .GroupBy(r => batchesById[r.BatchId].ProductId)
            .ToDictionary(
                g => g.Key,
                g => g.ToList());

        var transferGroups = transfers
            .Where(m => batchesById.TryGetValue(m.BatchId, out var batch)
                && verticalByProductId.TryGetValue(batch.ProductId, out _))
            .GroupBy(m => batchesById[m.BatchId].ProductId)
            .ToDictionary(
                g => g.Key,
                g => g.ToList());

        foreach (var productId in reservationGroups.Keys.Union(transferGroups.Keys))
        {
            try
            {
                await UpsertAsync(
                    productId,
                    verticalByProductId[productId],
                    day,
                    reservationGroups.GetValueOrDefault(productId) ?? new List<StockReservation>(),
                    transferGroups.GetValueOrDefault(productId) ?? new List<StockMovement>(),
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to aggregate the market signal for product {ProductId} on {Date}.",
                    productId,
                    day);
            }
        }

        await _dailyMarketSignalRepository.SaveChangesAsync(cancellationToken);
    }

    private async Task UpsertAsync(
        Guid productId,
        VerticalCategory verticalCategory,
        DateTime day,
        List<StockReservation> reservations,
        List<StockMovement> transfers,
        CancellationToken cancellationToken)
    {
        var stores = new HashSet<Guid>();
        foreach (var reservation in reservations)
        {
            stores.Add(reservation.RequestingStoreId);
            stores.Add(reservation.OwningStoreId);
        }

        foreach (var transfer in transfers)
        {
            stores.Add(transfer.StoreId);
            if (transfer.RelatedStoreId is { } related)
            {
                stores.Add(related);
            }
        }

        var signal = await _dailyMarketSignalRepository.GetByDateAsync(
            productId, verticalCategory, day, cancellationToken);

        if (signal is null)
        {
            signal = new DailyMarketSignal(productId, verticalCategory, day);
            await _dailyMarketSignalRepository.AddAsync(signal, cancellationToken);
        }

        signal.Update(
            reservations.Count,
            transfers.Sum(m => m.Quantity),
            stores.Count);
    }
}