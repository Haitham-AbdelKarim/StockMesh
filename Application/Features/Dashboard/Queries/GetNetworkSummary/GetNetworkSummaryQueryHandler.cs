using Application.Abstractions.Repositories;
using Application.Abstractions.Services;
using Application.Common.Models;
using Application.DTOs.Dashboard;
using Domain.Enums;
using MediatR;

namespace Application.Features.Dashboard.Queries.GetNetworkSummary;

public sealed class GetNetworkSummaryQueryHandler :
    IRequestHandler<GetNetworkSummaryQuery, Result<NetworkSummaryResponse>>
{
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IStockMovementRepository _stockMovementRepository;
    private readonly IStockReservationRepository _stockReservationRepository;

    public GetNetworkSummaryQueryHandler(
        ICurrentUser currentUser,
        IDateTimeProvider clock,
        IStockMovementRepository stockMovementRepository,
        IStockReservationRepository stockReservationRepository)
    {
        _currentUser = currentUser;
        _clock = clock;
        _stockMovementRepository = stockMovementRepository;
        _stockReservationRepository = stockReservationRepository;
    }

    public async Task<Result<NetworkSummaryResponse>> Handle(
        GetNetworkSummaryQuery query,
        CancellationToken cancellationToken)
    {
        var to = _clock.UtcNow.Date.AddDays(1);
        var from = to.AddDays(-query.Days);
        var storeId = _currentUser.StoreId;

        var movements = await _stockMovementRepository.GetForStoresAsync(
            [storeId], from, to, cancellationToken);

        var transfersOut = movements
            .Where(m => m.MovementType == MovementType.NetworkTransferOut)
            .ToList();

        var transfersIn = movements
            .Where(m => m.MovementType == MovementType.NetworkTransferIn)
            .ToList();

        var reservations = await _stockReservationRepository.GetForStoreByDateRangeAsync(
            storeId, from, to, cancellationToken);

        var resolved = reservations
            .Where(r => r.Status == ReservationStatus.Success || r.Status == ReservationStatus.Cancelled)
            .ToList();

        var successes = resolved.Count(r => r.Status == ReservationStatus.Success);

        var successRate = resolved.Count > 0
            ? Math.Round((decimal)successes / resolved.Count, 2)
            : 0m;

        var response = new NetworkSummaryResponse(
            transfersOut.Count,
            transfersIn.Count,
            transfersOut.Sum(m => m.Quantity),
            transfersIn.Sum(m => m.Quantity),
            transfersOut.Sum(m => m.Quantity * (m.UnitPrice ?? 0m)),
            transfersIn.Sum(m => m.Quantity * (m.UnitPrice ?? 0m)),
            successRate);

        return Result<NetworkSummaryResponse>.Success(response);
    }
}