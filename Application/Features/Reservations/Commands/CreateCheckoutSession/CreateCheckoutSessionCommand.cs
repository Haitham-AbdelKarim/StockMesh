using Application.Common.Models;
using Application.DTOs.Payments;
using MediatR;

namespace Application.Features.Reservations.Commands.CreateCheckoutSession;

public sealed record CreateCheckoutSessionCommand(
    Guid ReservationId) : IRequest<Result<CheckoutSessionResponse>>;