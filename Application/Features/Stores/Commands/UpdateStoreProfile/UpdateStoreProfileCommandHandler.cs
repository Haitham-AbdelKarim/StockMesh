using Application.Abstractions.Repositories;
using Application.Abstractions.Services;
using Application.Common.Models;
using Application.DTOs.Stores;
using Domain.Entities;
using MediatR;

namespace Application.Features.Stores.Commands.UpdateStoreProfile;

public sealed class UpdateStoreProfileCommandHandler :
    IRequestHandler<UpdateStoreProfileCommand, Result<StoreProfileResponse>>
{
    private readonly ICurrentUser _currentUser;
    private readonly IStoreRepository _storeRepository;

    public UpdateStoreProfileCommandHandler(
        ICurrentUser currentUser,
        IStoreRepository storeRepository)
    {
        _currentUser = currentUser;
        _storeRepository = storeRepository;
    }

    public async Task<Result<StoreProfileResponse>> Handle(
        UpdateStoreProfileCommand command,
        CancellationToken cancellationToken)
    {
        var store = await _storeRepository.GetByIdAsync(_currentUser.StoreId, cancellationToken);

        if (store is null)
        {
            return Result<StoreProfileResponse>.NotFound("Store not found.");
        }

        store.UpdateProfile(
            command.Name,
            command.Latitude,
            command.Longitude,
            command.MaxSearchRadiusKm);

        await _storeRepository.SaveChangesAsync(cancellationToken);

        return Result<StoreProfileResponse>.Success(ToResponse(store));
    }

    private static StoreProfileResponse ToResponse(Store store)
    {
        return new StoreProfileResponse(
            store.Id,
            store.Name,
            store.VerticalCategory,
            store.Latitude,
            store.Longitude,
            store.MaxSearchRadiusKm,
            store.IsVerified);
    }
}