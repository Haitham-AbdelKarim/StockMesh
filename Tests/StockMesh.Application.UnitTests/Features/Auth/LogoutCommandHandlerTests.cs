using Application.Abstractions.Models;
using Application.Abstractions.Repositories;
using Application.Abstractions.Services;
using Application.Features.Auth.Commands.Logout;
using Domain.Enums;
using FluentAssertions;

namespace StockMesh.Application.UnitTests.Features.Auth;

public class LogoutCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithKnownToken_ClearsItAndSucceeds()
    {
        var userRepository = new FakeStoreUserRepository();
        var handler = CreateHandler(userRepository);

        var result = await handler.Handle(
            new LogoutCommand("refresh-token"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        userRepository.ClearedHashes.Should().ContainSingle().Which.Should().Be("HASH:refresh-token");
    }

    [Fact]
    public async Task Handle_WithUnknownToken_SucceedsWithoutThrowing()
    {
        var userRepository = new FakeStoreUserRepository();
        var handler = CreateHandler(userRepository);

        var result = await handler.Handle(
            new LogoutCommand("unknown-token"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        userRepository.ClearedHashes.Should().ContainSingle();
    }

    [Fact]
    public async Task Validate_WithEmptyToken_Fails()
    {
        var validator = new LogoutCommandValidator();

        var result = validator.Validate(new LogoutCommand(string.Empty));

        result.IsValid.Should().BeFalse();
    }

    private static LogoutCommandHandler CreateHandler(FakeStoreUserRepository userRepository)
    {
        return new LogoutCommandHandler(userRepository, new FakeJwtTokenService());
    }

    private sealed class FakeStoreUserRepository : IStoreUserRepository
    {
        public List<string> ClearedHashes { get; } = new();

        public Task<StoreUserInfo?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<StoreUserInfo?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<StoreUserInfo?> ValidateCredentialsAsync(string email, string password, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<StoreUserInfo?> GetValidByRefreshTokenHashAsync(string refreshTokenHash, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<Guid> CreateOwnerAsync(Guid storeId, VerticalCategory verticalCategory, string email, string password, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<Guid> CreateStaffAsync(Guid storeId, VerticalCategory verticalCategory, string email, string password, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<bool> UpdateRefreshTokenAsync(Guid userId, string refreshTokenHash, DateTimeOffset expiresAt, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<bool> ClearRefreshTokenByHashAsync(string refreshTokenHash, CancellationToken cancellationToken = default)
        {
            ClearedHashes.Add(refreshTokenHash);

            return Task.FromResult(true);
        }
    }

    private sealed class FakeJwtTokenService : IJwtTokenService
    {
        public (string AccessToken, DateTimeOffset ExpiresAt) GenerateAccessToken(StoreUserInfo user)
        {
            throw new NotSupportedException();
        }

        public string GenerateRefreshToken()
        {
            throw new NotSupportedException();
        }

        public string HashRefreshToken(string refreshToken)
        {
            return $"HASH:{refreshToken}";
        }

        public DateTimeOffset GetRefreshTokenExpiry()
        {
            throw new NotSupportedException();
        }
    }
}