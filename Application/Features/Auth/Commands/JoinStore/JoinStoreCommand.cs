using Application.Common.Models;
using Application.DTOs.Auth;
using MediatR;

namespace Application.Features.Auth.Commands.JoinStore;

public sealed record JoinStoreCommand(string Email, string Password) : IRequest<Result<JoinStoreResponse>>;