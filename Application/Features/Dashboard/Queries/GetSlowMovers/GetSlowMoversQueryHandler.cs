using Application.Abstractions.Repositories;
using Application.Abstractions.Services;
using Application.Common.Models;
using Application.DTOs.Dashboard;
using MediatR;

namespace Application.Features.Dashboard.Queries.GetSlowMovers;

public sealed class GetSlowMoversQueryHandler :
    IRequestHandler<GetSlowMoversQuery, Result<IReadOnlyList<SlowMoverResponse>>>
{
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IDailyProductMetricRepository _dailyProductMetricRepository;
    private readonly IInventoryBatchRepository _inventoryBatchRepository;
    private readonly IProductRepository _productRepository;

    public GetSlowMoversQueryHandler(
        ICurrentUser currentUser,
        IDateTimeProvider clock,
        IDailyProductMetricRepository dailyProductMetricRepository,
        IInventoryBatchRepository inventoryBatchRepository,
        IProductRepository productRepository)
    {
        _currentUser = currentUser;
        _clock = clock;
        _dailyProductMetricRepository = dailyProductMetricRepository;
        _inventoryBatchRepository = inventoryBatchRepository;
        _productRepository = productRepository;
    }

    public async Task<Result<IReadOnlyList<SlowMoverResponse>>> Handle(
        GetSlowMoversQuery query,
        CancellationToken cancellationToken)
    {
        var to = _clock.UtcNow.Date.AddDays(1);
        var from = to.AddDays(-query.Days);
        var storeId = _currentUser.StoreId;

        var metrics = await _dailyProductMetricRepository.GetRangeAsync(
            storeId, from, to, cancellationToken);

        var byProduct = metrics
            .GroupBy(m => m.ProductId)
            .ToDictionary(
                g => g.Key,
                g => new
                {
                    UnitsSoldLast7Days = g
                        .Where(m => m.Date >= to.AddDays(-7))
                        .Sum(m => m.UnitsSold),
                    UnitsSoldLastDays = g.Sum(m => m.UnitsSold)
                });

        var stockSummaries = await _inventoryBatchRepository.GetProductStockSummariesAsync(
            storeId, cancellationToken);

        var onHandByProduct = stockSummaries
            .ToDictionary(s => s.ProductId, s => s.TotalQuantityRemaining);

        var slowMovers = byProduct
            .Where(x => x.Value.UnitsSoldLastDays > 0)
            .Select(x => new
            {
                x.Key,
                x.Value.UnitsSoldLast7Days,
                x.Value.UnitsSoldLastDays,
                QuantityRemaining = onHandByProduct.GetValueOrDefault(x.Key)
            })
            .Where(x => x.QuantityRemaining > 0)
            .OrderBy(x => (double)x.UnitsSoldLastDays / query.Days)
            .ThenBy(x => x.Key)
            .ToList();

        if (slowMovers.Count > 0)
        {
            var products = await _productRepository.GetByIdsAsync(
                slowMovers.Select(x => x.Key).ToList(),
                cancellationToken);

            var productNames = products.ToDictionary(p => p.Id, p => p.Name);

            var response = slowMovers
                .Select(x =>
                {
                    var averageDaily = (double)x.UnitsSoldLastDays / query.Days;

                    return new SlowMoverResponse(
                        x.Key,
                        productNames.GetValueOrDefault(x.Key) ?? string.Empty,
                        x.UnitsSoldLast7Days,
                        x.UnitsSoldLastDays,
                        x.QuantityRemaining,
                        (int)Math.Floor(x.QuantityRemaining / averageDaily));
                })
                .ToList();

            return Result<IReadOnlyList<SlowMoverResponse>>.Success(response);
        }

        return Result<IReadOnlyList<SlowMoverResponse>>.Success([]);
    }
}