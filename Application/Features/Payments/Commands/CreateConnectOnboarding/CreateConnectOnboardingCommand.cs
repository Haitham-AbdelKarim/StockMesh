using Application.Common.Models;
using Application.DTOs.Payments;
using MediatR;

namespace Application.Features.Payments.Commands.CreateConnectOnboarding;

public sealed record CreateConnectOnboardingCommand : IRequest<Result<ConnectOnboardingResponse>>;