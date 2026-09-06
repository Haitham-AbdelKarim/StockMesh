using MediatR;

namespace Application.Features.Reservations.Notifications;

public sealed record ReservationResolvedSuccessfullyNotification(Guid ReservationId) : INotification;