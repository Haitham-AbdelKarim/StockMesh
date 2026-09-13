using Application.Common.Models;
using Application.DTOs.Payments;
using MediatR;

namespace Application.Features.Payments.Queries.GetConnectStatus;

public sealed record GetConnectStatusQuery : IRequest<Result<ConnectStatusResponse>>;