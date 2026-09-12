using Application.Common.Models;
using Application.DTOs.Auth;
using MediatR;

namespace Application.Features.Auth.Commands.Logout;

public sealed record LogoutCommand(string RefreshToken) : IRequest<Result<LogoutResponse>>;