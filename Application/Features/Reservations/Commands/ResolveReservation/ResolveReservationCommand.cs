using Application.Common.Models;
using Application.DTOs.Reservations;
using Domain.Enums;
using MediatR;

namespace Application.Features.Reservations.Commands.ResolveReservation;

public sealed record ResolveReservationCommand(
    Guid ReservationId,
    ReservationStatus Outcome) : IRequest<Result<ReservationResponse>>;