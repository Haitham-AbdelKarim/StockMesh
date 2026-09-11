using Application.Abstractions.Repositories;
using Application.Abstractions.Services;
using Application.Common.Models;
using Application.DTOs.Recommendations;
using MediatR;

namespace Application.Features.Recommendations.Queries.GetMarketHistory;

public sealed class GetMarketHistoryQueryHandler :
    IRequestHandler<GetMarketHistoryQuery, Result<IReadOnlyList<MarketSignalPointResponse>>>
{
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IDailyMarketSignalRepository _marketSignalRepository;

    public GetMarketHistoryQueryHandler(
        ICurrentUser currentUser,
        IDateTimeProvider clock,
        IDailyMarketSignalRepository marketSignalRepository)
    {
        _currentUser = currentUser;
        _clock = clock;
        _marketSignalRepository = marketSignalRepository;
    }

    public async Task<Result<IReadOnlyList<MarketSignalPointResponse>>> Handle(
        GetMarketHistoryQuery query,
        CancellationToken cancellationToken)
    {
        var to = _clock.UtcNow.Date.AddDays(1);
        var history = await _marketSignalRepository.GetHistoryAsync(
            query.ProductId,
            _currentUser.VerticalCategory,
            to.AddDays(-query.Days),
            to,
            cancellationToken);

        IReadOnlyList<MarketSignalPointResponse> points = history
            .Select(s => new MarketSignalPointResponse(
                s.Date,
                s.ReservationCount,
                s.TransferVolume,
                s.ParticipatingStoreCount))
            .ToList();

        return Result<IReadOnlyList<MarketSignalPointResponse>>.Success(points);
    }
}