using Application.Abstractions.Models;
using Domain.Enums;
using Infrastructure.Identity;
using Microsoft.Extensions.Options;

namespace StockMesh.Api.IntegrationTests;

internal static class TestAuth
{
    private const string TestKey = "8d26a0177d174f9b3084920b7afc9b15c9246c3676403fc618ad2804ad319ccf";

    public static string CreateAccessToken(Guid storeId)
    {
        var settings = Options.Create(new JwtSettings
        {
            Key = TestKey,
            Issuer = "StockMesh",
            Audience = "StockMesh",
            AccessTokenExpirationMinutes = 15,
            RefreshTokenExpirationDays = 7
        });

        var service = new JwtTokenService(settings);

        return service.GenerateAccessToken(new StoreUserInfo(
            Guid.NewGuid(),
            storeId,
            "owner@test.local",
            StoreUserRole.Owner.ToString(),
            VerticalCategory.Gaming)).AccessToken;
    }
}