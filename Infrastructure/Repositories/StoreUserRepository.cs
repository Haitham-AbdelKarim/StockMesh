using Application.Abstractions.Models;
using Application.Abstractions.Repositories;
using Domain.Enums;
using Infrastructure.Identity;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class StoreUserRepository : IStoreUserRepository
{
    private readonly AppDbContext _dbContext;
    private readonly UserManager<StoreUser> _userManager;

    public StoreUserRepository(
        AppDbContext dbContext,
        UserManager<StoreUser> userManager)
    {
        _dbContext = dbContext;
        _userManager = userManager;
    }

    public async Task<StoreUserInfo?> GetByEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(email);
        return user is null ? null : await ToInfoAsync(user, cancellationToken);
    }

    public async Task<StoreUserInfo?> GetByIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(
            u => u.Id == userId,
            cancellationToken);
        return user is null ? null : await ToInfoAsync(user, cancellationToken);
    }

    public async Task<StoreUserInfo?> ValidateCredentialsAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            return null;
        }

        return await _userManager.CheckPasswordAsync(user, password)
            ? await ToInfoAsync(user, cancellationToken)
            : null;
    }

    public async Task<StoreUserInfo?> GetValidByRefreshTokenHashAsync(
        string refreshTokenHash,
        CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(
            u => u.RefreshTokenHash == refreshTokenHash
                 && u.RefreshTokenExpiresAt > DateTimeOffset.UtcNow,
            cancellationToken);
        return user is null ? null : await ToInfoAsync(user, cancellationToken);
    }

    public async Task<bool> EmailExistsAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        return await _userManager.FindByEmailAsync(email) is not null;
    }

    public async Task<Guid> CreateOwnerAsync(
        Guid storeId,
        VerticalCategory verticalCategory,
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        return await CreateUserAsync(
            storeId,
            verticalCategory,
            email,
            password,
            StoreUserRole.Owner.ToString(),
            cancellationToken);
    }

    public async Task<Guid> CreateStaffAsync(
        Guid storeId,
        VerticalCategory verticalCategory,
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        return await CreateUserAsync(
            storeId,
            verticalCategory,
            email,
            password,
            StoreUserRole.Staff.ToString(),
            cancellationToken);
    }

    public async Task<bool> UpdateRefreshTokenAsync(
        Guid userId,
        string refreshTokenHash,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users.FindAsync([userId], cancellationToken);
        if (user is null)
        {
            return false;
        }

        user.RefreshTokenHash = refreshTokenHash;
        user.RefreshTokenExpiresAt = expiresAt;
        user.RefreshTokenCreatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<Guid> CreateUserAsync(
        Guid storeId,
        VerticalCategory verticalCategory,
        string email,
        string password,
        string role,
        CancellationToken cancellationToken)
    {
        var user = new StoreUser
        {
            Id = Guid.NewGuid(),
            StoreId = storeId,
            VerticalCategory = verticalCategory,
            Email = email,
            UserName = email,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                string.Join("; ", result.Errors.Select(e => e.Description)));
        }

        var roleResult = await _userManager.AddToRoleAsync(user, role);
        if (!roleResult.Succeeded)
        {
            throw new InvalidOperationException(
                string.Join("; ", roleResult.Errors.Select(e => e.Description)));
        }

        return user.Id;
    }

    private async Task<StoreUserInfo> ToInfoAsync(
        StoreUser user,
        CancellationToken cancellationToken)
    {
        var roles = await _userManager.GetRolesAsync(user);

        return new StoreUserInfo(
            user.Id,
            user.StoreId,
            user.Email ?? string.Empty,
            roles.FirstOrDefault() ?? string.Empty,
            user.VerticalCategory);
    }
}