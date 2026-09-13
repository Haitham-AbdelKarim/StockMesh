using Application.Abstractions.Repositories;
using Application.Abstractions.Services;
using Domain.Entities;
using Domain.Enums;

namespace Application.Features.Metrics.Services;

public sealed class DailyMetricsMaterializer : IDailyMetricsMaterializer
{
    private readonly IStockMovementRepository _stockMovementRepository;
    private readonly IExpenseRepository _expenseRepository;
    private readonly IInventoryBatchRepository _inventoryBatchRepository;
    private readonly IDailyStoreMetricRepository _dailyStoreMetricRepository;
    private readonly IDailyProductMetricRepository _dailyProductMetricRepository;

    public DailyMetricsMaterializer(
        IStockMovementRepository stockMovementRepository,
        IExpenseRepository expenseRepository,
        IInventoryBatchRepository inventoryBatchRepository,
        IDailyStoreMetricRepository dailyStoreMetricRepository,
        IDailyProductMetricRepository dailyProductMetricRepository)
    {
        _stockMovementRepository = stockMovementRepository;
        _expenseRepository = expenseRepository;
        _inventoryBatchRepository = inventoryBatchRepository;
        _dailyStoreMetricRepository = dailyStoreMetricRepository;
        _dailyProductMetricRepository = dailyProductMetricRepository;
    }

    public async Task RecomputeDayAsync(
        IReadOnlyCollection<Guid> storeIds,
        DateTime date,
        CancellationToken cancellationToken)
    {
        if (storeIds.Count == 0)
        {
            return;
        }

        var from = date.Date;
        var to = from.AddDays(1);

        var movements = await _stockMovementRepository.GetForStoresAsync(
            storeIds, from, to, cancellationToken);

        var expenses = await _expenseRepository.GetForStoresAsync(
            storeIds, from, to, cancellationToken);

        var expenseTotalsByStore = expenses
            .GroupBy(e => e.StoreId)
            .ToDictionary(g => g.Key, g => g.Sum(e => e.Amount));

        var productIdsByBatch = await LoadProductIdsByBatchAsync(movements, cancellationToken);

        var storeMetricCache = new Dictionary<Guid, DailyStoreMetric>();
        var productMetricCache = new Dictionary<(Guid StoreId, Guid ProductId), DailyProductMetric>();

        var eligibleMovements = movements
            .Where(m => m.MovementType is MovementType.Sale
                or MovementType.NetworkTransferOut
                or MovementType.NetworkTransferIn
                or MovementType.Restock)
            .ToList();

        foreach (var storeGroup in eligibleMovements.GroupBy(m => m.StoreId))
        {
            var storeId = storeGroup.Key;
            var sales = storeGroup.Where(m => m.MovementType == MovementType.Sale).ToList();
            var transfersOut = storeGroup.Where(m => m.MovementType == MovementType.NetworkTransferOut).ToList();
            var transfersIn = storeGroup.Where(m => m.MovementType == MovementType.NetworkTransferIn).ToList();
            var restocks = storeGroup.Where(m => m.MovementType == MovementType.Restock).ToList();

            var storeMetric = await GetOrCreateStoreMetricAsync(
                storeMetricCache, storeId, from, cancellationToken);

            storeMetric.Update(
                salesRevenue: sales.Sum(m => m.Quantity * (m.UnitPrice ?? 0m)),
                transfersOutRevenue: transfersOut.Sum(m => m.Quantity * (m.UnitPrice ?? 0m)),
                costOfGoodsSold: sales.Sum(m => m.Quantity * (m.UnitCost ?? 0m))
                    + transfersOut.Sum(m => m.Quantity * (m.UnitCost ?? 0m)),
                stockPurchases: restocks.Sum(m => m.Quantity * (m.UnitCost ?? 0m))
                    + transfersIn.Sum(m => m.Quantity * (m.UnitPrice ?? m.UnitCost ?? 0m)),
                expenseTotal: expenseTotalsByStore.GetValueOrDefault(storeId),
                unitsSold: sales.Sum(m => m.Quantity),
                transfersOutUnits: transfersOut.Sum(m => m.Quantity),
                transfersInUnits: transfersIn.Sum(m => m.Quantity));

            foreach (var productGroup in storeGroup.GroupBy(m => productIdsByBatch.GetValueOrDefault(m.BatchId)))
            {
                if (productGroup.Key == Guid.Empty)
                {
                    continue;
                }

                var productId = productGroup.Key;
                var productMetric = await GetOrCreateProductMetricAsync(
                    productMetricCache,
                    storeId,
                    productId,
                    from,
                    cancellationToken);

                productMetric.Update(
                    unitsSold: productGroup
                        .Where(m => m.MovementType == MovementType.Sale)
                        .Sum(m => m.Quantity),
                    salesRevenue: productGroup
                        .Where(m => m.MovementType == MovementType.Sale)
                        .Sum(m => m.Quantity * (m.UnitPrice ?? 0m)),
                    salesCost: productGroup
                        .Where(m => m.MovementType == MovementType.Sale)
                        .Sum(m => m.Quantity * (m.UnitCost ?? 0m)),
                    transfersOutUnits: productGroup
                        .Where(m => m.MovementType == MovementType.NetworkTransferOut)
                        .Sum(m => m.Quantity),
                    transfersOutRevenue: productGroup
                        .Where(m => m.MovementType == MovementType.NetworkTransferOut)
                        .Sum(m => m.Quantity * (m.UnitPrice ?? 0m)));
            }
        }

        var storesWithExpensesOnly = expenseTotalsByStore.Keys
            .Where(storeId => !storeMetricCache.ContainsKey(storeId));

        foreach (var storeId in storesWithExpensesOnly)
        {
            var storeMetric = await GetOrCreateStoreMetricAsync(
                storeMetricCache, storeId, from, cancellationToken);

            storeMetric.Update(0m, 0m, 0m, 0m, expenseTotalsByStore[storeId], 0, 0, 0);
        }
    }

    private async Task<IReadOnlyDictionary<Guid, Guid>> LoadProductIdsByBatchAsync(
        IEnumerable<StockMovement> movements,
        CancellationToken cancellationToken)
    {
        var batchIds = movements.Select(m => m.BatchId).Distinct().ToList();

        if (batchIds.Count == 0)
        {
            return new Dictionary<Guid, Guid>();
        }

        var batches = await _inventoryBatchRepository.GetByIdsAsync(batchIds, cancellationToken);

        return batches.ToDictionary(b => b.Id, b => b.ProductId);
    }

    private async Task<DailyStoreMetric> GetOrCreateStoreMetricAsync(
        IDictionary<Guid, DailyStoreMetric> cache,
        Guid storeId,
        DateTime date,
        CancellationToken cancellationToken)
    {
        if (cache.TryGetValue(storeId, out var cached))
        {
            return cached;
        }

        var metric = await _dailyStoreMetricRepository.GetAsync(storeId, date, cancellationToken);

        if (metric is null)
        {
            metric = new DailyStoreMetric(storeId, date);
            await _dailyStoreMetricRepository.AddAsync(metric, cancellationToken);
        }
        else
        {
            _dailyStoreMetricRepository.Update(metric);
        }

        cache[storeId] = metric;

        return metric;
    }

    private async Task<DailyProductMetric> GetOrCreateProductMetricAsync(
        IDictionary<(Guid StoreId, Guid ProductId), DailyProductMetric> cache,
        Guid storeId,
        Guid productId,
        DateTime date,
        CancellationToken cancellationToken)
    {
        var key = (storeId, productId);

        if (cache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        var metric = await _dailyProductMetricRepository.GetAsync(storeId, productId, date, cancellationToken);

        if (metric is null)
        {
            metric = new DailyProductMetric(storeId, productId, date);
            await _dailyProductMetricRepository.AddAsync(metric, cancellationToken);
        }
        else
        {
            _dailyProductMetricRepository.Update(metric);
        }

        cache[key] = metric;

        return metric;
    }
}