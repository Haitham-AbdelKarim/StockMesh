using System.IdentityModel.Tokens.Jwt;
using Application.Abstractions.Models;
using Domain.Enums;
using FluentAssertions;
using Infrastructure.Identity;
using Microsoft.Extensions.Options;

namespace StockMesh.Infrastructure.UnitTests.Identity;

public class JwtTokenServiceTests
{
    private const string TestKey = "8d26a0177d174f9b3084920b7afc9b15c9246c3676403fc618ad2804ad319ccf";

    private static JwtTokenService CreateService()
    {
        var settings = new JwtSettings
        {
            Key = TestKey,
            Issuer = "StockMesh",
            Audience = "StockMesh",
            AccessTokenExpirationMinutes = 15,
            RefreshTokenExpirationDays = 7
        };

        return new JwtTokenService(Options.Create(settings));
    }

    [Fact]
    public void GenerateAccessToken_IncludesExpectedClaims()
    {
        var service = CreateService();
        var user = new StoreUserInfo(
            Guid.Parse("1b4559c6-9e9a-4e22-9b2e-8c10c3d4e5f6"),
            Guid.Parse("2c5660d7-8f0a-4f33-8c3f-9d21d4e5f6a7"),
            "owner@store.com",
            "Owner",
            VerticalCategory.Gaming);

        var (accessToken, expiresAt) = service.GenerateAccessToken(user);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);
        jwt.Payload["sub"].Should().Be(user.UserId.ToString());
        jwt.Payload["store_id"].Should().Be(user.StoreId.ToString());
        jwt.Payload["email"].Should().Be(user.Email);
        jwt.Payload["role"].Should().Be("Owner");
        jwt.Payload["vertical_category"].Should().Be("Gaming");
        jwt.Payload["iss"].Should().Be("StockMesh");
        jwt.Payload["aud"].Should().Be("StockMesh");

        expiresAt.Should().BeCloseTo(DateTimeOffset.UtcNow.AddMinutes(15), TimeSpan.FromMinutes(1));
    }

    [Fact]
    public void GenerateRefreshToken_YieldsUniqueValues()
    {
        var service = CreateService();

        var first = service.GenerateRefreshToken();
        var second = service.GenerateRefreshToken();

        first.Should().NotBeNullOrWhiteSpace();
        second.Should().NotBeNullOrWhiteSpace();
        first.Should().NotBe(second);
    }

    [Fact]
    public void HashRefreshToken_ReturnsDeterministicSha256Hex()
    {
        var service = CreateService();

        var first = service.HashRefreshToken("a-refresh-token-value");
        var second = service.HashRefreshToken("a-refresh-token-value");

        first.Should().Be(second);
        first.Should().MatchRegex("^[0-9A-F]{64}$");
    }

    [Fact]
    public void HashRefreshToken_StoresDigestNotRawValue()
    {
        var service = CreateService();

        var raw = service.GenerateRefreshToken();
        var hash = service.HashRefreshToken(raw);

        hash.Should().NotBe(raw);
    }

    [Fact]
    public void GetRefreshTokenExpiry_IsSevenDaysFromNow()
    {
        var service = CreateService();

        service.GetRefreshTokenExpiry()
            .Should()
            .BeCloseTo(DateTimeOffset.UtcNow.AddDays(7), TimeSpan.FromMinutes(1));
    }
}