using Application.Abstractions.Models;
using Application.Abstractions.Repositories;
using Application.Abstractions.Services;
using Application.Common.Models;
using Application.DTOs.Auth;
using MediatR;

namespace Application.Features.Auth.Commands.Refresh;

public sealed class RefreshCommandHandler : IRequestHandler<RefreshCommand, Result<TokenResponse>>
{
    private readonly IStoreUserRepository _userRepository;
    private readonly IJwtTokenService _jwtTokens;

    public RefreshCommandHandler(
        IStoreUserRepository userRepository,
        IJwtTokenService jwtTokens)
    {
        _userRepository = userRepository;
        _jwtTokens = jwtTokens;
    }

    public async Task<Result<TokenResponse>> Handle(
        RefreshCommand command,
        CancellationToken cancellationToken)
    {
        var refreshTokenHash = _jwtTokens.HashRefreshToken(command.RefreshToken);

        var user = await _userRepository.GetValidByRefreshTokenHashAsync(
            refreshTokenHash,
            cancellationToken);

        if (user is null)
        {
            return Result<TokenResponse>.Unauthorized("The refresh token is invalid or has expired.");
        }

        var (accessToken, expiresAt) = _jwtTokens.GenerateAccessToken(user);

        var refreshToken = _jwtTokens.GenerateRefreshToken();
        var newRefreshTokenHash = _jwtTokens.HashRefreshToken(refreshToken);

        await _userRepository.UpdateRefreshTokenAsync(
            user.UserId,
            newRefreshTokenHash,
            _jwtTokens.GetRefreshTokenExpiry(),
            cancellationToken);

        return Result<TokenResponse>.Success(new TokenResponse(accessToken, refreshToken, expiresAt));
    }
}