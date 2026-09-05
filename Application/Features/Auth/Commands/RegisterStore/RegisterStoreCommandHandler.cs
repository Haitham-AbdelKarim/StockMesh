using Application.Abstractions.Models;
using Application.Abstractions.Repositories;
using Application.Abstractions.Services;
using Application.DTOs.Auth;
using Application.Exceptions;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Features.Auth.Commands.RegisterStore;

public sealed class RegisterStoreCommandHandler : IRequestHandler<RegisterStoreCommand, RegisterStoreResponse>
{
    private readonly IStoreRepository _storeRepository;
    private readonly IStoreUserRepository _userRepository;
    private readonly IJwtTokenService _jwtTokens;

    public RegisterStoreCommandHandler(
        IStoreRepository storeRepository,
        IStoreUserRepository userRepository,
        IJwtTokenService jwtTokens)
    {
        _storeRepository = storeRepository;
        _userRepository = userRepository;
        _jwtTokens = jwtTokens;
    }

    public async Task<RegisterStoreResponse> Handle(
        RegisterStoreCommand command,
        CancellationToken cancellationToken)
    {
        if (await _userRepository.EmailExistsAsync(command.Email, cancellationToken))
        {
            throw new ConflictException("A user with this email already exists.");
        }

        var store = new Store(
            command.StoreName,
            command.VerticalCategory,
            command.Latitude,
            command.Longitude,
            command.MaxSearchRadiusKm);

        await _storeRepository.AddAsync(store, cancellationToken);
        await _storeRepository.SaveChangesAsync(cancellationToken);

        Guid ownerId;
        try
        {
            ownerId = await _userRepository.CreateOwnerAsync(
                store.Id,
                command.VerticalCategory,
                command.Email,
                command.Password,
                cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            throw new BadRequestException(ex.Message);
        }

        var owner = new StoreUserInfo(
            ownerId,
            store.Id,
            command.Email,
            StoreUserRole.Owner.ToString(),
            command.VerticalCategory);

        var tokens = await IssueTokenPairAsync(owner, cancellationToken);

        return new RegisterStoreResponse(store.Id, ownerId, tokens);
    }

    private async Task<TokenResponse> IssueTokenPairAsync(
        StoreUserInfo user,
        CancellationToken cancellationToken)
    {
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