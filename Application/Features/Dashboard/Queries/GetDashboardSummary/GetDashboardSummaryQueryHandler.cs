using Application.Abstractions.Repositories;
using Application.Abstractions.Services;
using Application.Common.Models;
using Application.DTOs.Dashboard;
using MediatR;

namespace Application.Features.Dashboard.Queries.GetDashboardSummary;

public sealed class GetDashboardSummaryQueryHandler :
    IRequestHandler<GetDashboardSummaryQuery, Result<DashboardSummaryResponse>>
{
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IDailyStoreMetricRepository _dailyStoreMetricRepository;

    public GetDashboardSummaryQueryHandler(
        ICurrentUser currentUser,
        IDateTimeProvider clock,
        IDailyStoreMetricRepository dailyStoreMetricRepository)
    {
        _currentUser = currentUser;
        _clock = clock;
        _dailyStoreMetricRepository = dailyStoreMetricRepository;
    }

    public async Task<Result<DashboardSummaryResponse>> Handle(
        GetDashboardSummaryQuery query,
        CancellationToken cancellationToken)
    {
        var date = (query.Date ?? _clock.UtcNow).Date;

        var metric = await _dailyStoreMetricRepository.GetAsync(
            _currentUser.StoreId,
            date,
            cancellationToken);

        var response = metric is null
            ? new DashboardSummaryResponse(
                date,
                0m,
                0m,
                0m,
                0m,
                0m,
                0m,
                0,
                0,
                0)
            : new DashboardSummaryResponse(
                metric.Date,
                metric.SalesRevenue,
                metric.TransfersOutRevenue,
                metric.CostOfGoodsSold,
                metric.StockPurchases,
                metric.ExpenseTotal,
                metric.NetProfit,
                metric.UnitsSold,
                metric.TransfersOutUnits,
                metric.TransfersInUnits);

        return Result<DashboardSummaryResponse>.Success(response);
    }
}