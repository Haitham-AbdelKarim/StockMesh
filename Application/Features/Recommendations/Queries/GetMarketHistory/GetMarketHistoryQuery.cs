using Application.Common.Models;
using Application.DTOs.Recommendations;
using MediatR;

namespace Application.Features.Recommendations.Queries.GetMarketHistory;

public sealed record GetMarketHistoryQuery(
    Guid ProductId,
    int Days = 90) : IRequest<Result<IReadOnlyList<MarketSignalPointResponse>>>;