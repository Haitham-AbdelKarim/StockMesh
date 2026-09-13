using Application.Abstractions.Repositories;
using Application.Abstractions.Services;
using Application.Common.Models;
using Application.DTOs.Dashboard;
using MediatR;

namespace Application.Features.Dashboard.Queries.GetTopSellers;

public sealed class GetTopSellersQueryHandler :
    IRequestHandler<GetTopSellersQuery, Result<IReadOnlyList<TopSellerResponse>>>
{
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IDailyProductMetricRepository _dailyProductMetricRepository;
    private readonly IProductRepository _productRepository;

    public GetTopSellersQueryHandler(
        ICurrentUser currentUser,
        IDateTimeProvider clock,
        IDailyProductMetricRepository dailyProductMetricRepository,
        IProductRepository productRepository)
    {
        _currentUser = currentUser;
        _clock = clock;
        _dailyProductMetricRepository = dailyProductMetricRepository;
        _productRepository = productRepository;
    }

    public async Task<Result<IReadOnlyList<TopSellerResponse>>> Handle(
        GetTopSellersQuery query,
        CancellationToken cancellationToken)
    {
        var to = (query.To ?? _clock.UtcNow).Date.AddDays(1);
        var from = query.From ?? to.AddDays(-30);

        var metrics = await _dailyProductMetricRepository.GetRangeAsync(
            _currentUser.StoreId,
            from,
            to,
            cancellationToken);

        var aggregated = metrics
            .GroupBy(m => m.ProductId)
            .Select(g => new
            {
                ProductId = g.Key,
                UnitsSold = g.Sum(m => m.UnitsSold),
                SalesRevenue = g.Sum(m => m.SalesRevenue)
            })
            .Where(x => x.UnitsSold > 0)
            .OrderByDescending(x => x.UnitsSold)
            .Take(query.TopN)
            .ToList();

        var products = await LoadProductNamesAsync(
            aggregated.Select(x => x.ProductId).ToList(),
            cancellationToken);

        var response = aggregated
            .Select(x => new TopSellerResponse(
                x.ProductId,
                products.GetValueOrDefault(x.ProductId) ?? string.Empty,
                x.UnitsSold,
                x.SalesRevenue))
            .ToList();

        return Result<IReadOnlyList<TopSellerResponse>>.Success(response);
    }

    private async Task<IReadOnlyDictionary<Guid, string>> LoadProductNamesAsync(
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken)
    {
        if (productIds.Count == 0)
        {
            return new Dictionary<Guid, string>();
        }

        var products = await _productRepository.GetByIdsAsync(productIds, cancellationToken);

        return products.ToDictionary(p => p.Id, p => p.Name);
    }
}