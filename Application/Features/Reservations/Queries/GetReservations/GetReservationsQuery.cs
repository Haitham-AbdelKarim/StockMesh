using Application.Common.Models;
using Application.DTOs.Reservations;
using Domain.Enums;
using MediatR;

namespace Application.Features.Reservations.Queries.GetReservations;

public sealed record GetReservationsQuery(
    ReservationStatus? Status = null,
    bool? Incoming = null,
    DateTime? From = null,
    DateTime? To = null,
    int Page = 1,
    int PageSize = 20) : IRequest<Result<PaginatedList<ReservationDetailResponse>>>;