using Application.Abstractions.Repositories;
using Application.Abstractions.Services;
using Application.Common.Models;
using Application.DTOs.Auth;
using MediatR;

namespace Application.Features.Auth.Commands.Logout;

public sealed class LogoutCommandHandler : IRequestHandler<LogoutCommand, Result<LogoutResponse>>
{
    private readonly IStoreUserRepository _userRepository;
    private readonly IJwtTokenService _jwtTokens;

    public LogoutCommandHandler(
        IStoreUserRepository userRepository,
        IJwtTokenService jwtTokens)
    {
        _userRepository = userRepository;
        _jwtTokens = jwtTokens;
    }

    public async Task<Result<LogoutResponse>> Handle(
        LogoutCommand command,
        CancellationToken cancellationToken)
    {
        var refreshTokenHash = _jwtTokens.HashRefreshToken(command.RefreshToken);

        // Global revoke, idempotent by design: an unknown or already-cleared
        // token still succeeds so logout can never leak token existence.
        await _userRepository.ClearRefreshTokenByHashAsync(refreshTokenHash, cancellationToken);

        return Result<LogoutResponse>.Success(new LogoutResponse());
    }
}