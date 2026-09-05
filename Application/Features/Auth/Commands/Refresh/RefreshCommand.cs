using Application.Common.Models;
using Application.DTOs.Auth;
using MediatR;

namespace Application.Features.Auth.Commands.Refresh;

public sealed record RefreshCommand(string RefreshToken) : IRequest<Result<TokenResponse>>;