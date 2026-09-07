using Application.Abstractions.Repositories;
using Application.Abstractions.Services;
using Application.Common.Models;
using Application.DTOs.Dashboard;
using MediatR;

namespace Application.Features.Dashboard.Queries.GetSalesTrend;

public sealed class GetSalesTrendQueryHandler :
    IRequestHandler<GetSalesTrendQuery, Result<IReadOnlyList<SalesTrendPointResponse>>>
{
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IDailyStoreMetricRepository _dailyStoreMetricRepository;

    public GetSalesTrendQueryHandler(
        ICurrentUser currentUser,
        IDateTimeProvider clock,
        IDailyStoreMetricRepository dailyStoreMetricRepository)
    {
        _currentUser = currentUser;
        _clock = clock;
        _dailyStoreMetricRepository = dailyStoreMetricRepository;
    }

    public async Task<Result<IReadOnlyList<SalesTrendPointResponse>>> Handle(
        GetSalesTrendQuery query,
        CancellationToken cancellationToken)
    {
        var to = _clock.UtcNow.Date.AddDays(1);
        var from = to.AddDays(-query.Days);
        var storeId = _currentUser.StoreId;

        var metrics = await _dailyStoreMetricRepository.GetRangeAsync(
            storeId, from, to, cancellationToken);

        var byDate = metrics.ToDictionary(m => m.Date, m => m);

        var points = new List<SalesTrendPointResponse>(query.Days);

        for (var date = from; date < to; date = date.AddDays(1))
        {
            var metric = byDate.GetValueOrDefault(date);

            points.Add(new SalesTrendPointResponse(
                date,
                metric?.UnitsSold ?? 0,
                metric?.SalesRevenue ?? 0m));
        }

        return Result<IReadOnlyList<SalesTrendPointResponse>>.Success(points);
    }
}