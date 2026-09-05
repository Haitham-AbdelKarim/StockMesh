using Application.Abstractions.Models;

namespace Application.Abstractions.Services;

public interface IJwtTokenService
{
    (string AccessToken, DateTimeOffset ExpiresAt) GenerateAccessToken(StoreUserInfo user);

    string GenerateRefreshToken();

    string HashRefreshToken(string refreshToken);

    DateTimeOffset GetRefreshTokenExpiry();
}