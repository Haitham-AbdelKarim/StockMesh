using Application.Abstractions.Models;
using Application.Abstractions.Repositories;
using Application.Abstractions.Services;
using Application.DTOs.Auth;
using Application.Exceptions;
using MediatR;

namespace Application.Features.Auth.Commands.Login;

public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, TokenResponse>
{
    private readonly IStoreUserRepository _userRepository;
    private readonly IJwtTokenService _jwtTokens;

    public LoginCommandHandler(
        IStoreUserRepository userRepository,
        IJwtTokenService jwtTokens)
    {
        _userRepository = userRepository;
        _jwtTokens = jwtTokens;
    }

    public async Task<TokenResponse> Handle(
        LoginCommand command,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.ValidateCredentialsAsync(
            command.Email,
            command.Password,
            cancellationToken);

        if (user is null)
        {
            throw new UnauthorizedException("Invalid email or password.");
        }

        var (accessToken, expiresAt) = _jwtTokens.GenerateAccessToken(user);

        var refreshToken = _jwtTokens.GenerateRefreshToken();
        var refreshTokenHash = _jwtTokens.HashRefreshToken(refreshToken);

        await _userRepository.UpdateRefreshTokenAsync(
            user.UserId,
            refreshTokenHash,
            _jwtTokens.GetRefreshTokenExpiry(),
            cancellationToken);

        return new TokenResponse(accessToken, refreshToken, expiresAt);
    }
}