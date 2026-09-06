using Application.Common.Models;
using Application.DTOs.Reservations;
using MediatR;

namespace Application.Features.Reservations.Commands.ReserveStock;

public sealed record ReserveStockCommand(
    Guid BatchId,
    int Quantity,
    decimal? DistanceKm = null,
    DateTime? DeliveryEta = null) : IRequest<Result<ReservationResponse>>;