using Application.Abstractions.Models;
using Domain.Enums;

namespace Application.Abstractions.Repositories;

public interface IStoreUserRepository
{
    Task<StoreUserInfo?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<StoreUserInfo?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<StoreUserInfo?> ValidateCredentialsAsync(string email, string password, CancellationToken cancellationToken = default);

    Task<StoreUserInfo?> GetValidByRefreshTokenHashAsync(string refreshTokenHash, CancellationToken cancellationToken = default);

    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);

    Task<Guid> CreateOwnerAsync(Guid storeId, VerticalCategory verticalCategory, string email, string password, CancellationToken cancellationToken = default);

    Task<Guid> CreateStaffAsync(Guid storeId, VerticalCategory verticalCategory, string email, string password, CancellationToken cancellationToken = default);

    Task<bool> UpdateRefreshTokenAsync(Guid userId, string refreshTokenHash, DateTimeOffset expiresAt, CancellationToken cancellationToken = default);

    Task<bool> ClearRefreshTokenByHashAsync(string refreshTokenHash, CancellationToken cancellationToken = default);
}