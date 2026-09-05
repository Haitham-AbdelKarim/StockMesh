using Application.Abstractions.Repositories;
using Application.Abstractions.Services;
using Application.Common.Models;
using Application.DTOs.Stores;
using Domain.Entities;
using MediatR;

namespace Application.Features.Stores.Queries.GetStoreProfile;

public sealed class GetStoreProfileQueryHandler :
    IRequestHandler<GetStoreProfileQuery, Result<StoreProfileResponse>>
{
    private readonly ICurrentUser _currentUser;
    private readonly IStoreRepository _storeRepository;

    public GetStoreProfileQueryHandler(
        ICurrentUser currentUser,
        IStoreRepository storeRepository)
    {
        _currentUser = currentUser;
        _storeRepository = storeRepository;
    }

    public async Task<Result<StoreProfileResponse>> Handle(
        GetStoreProfileQuery query,
        CancellationToken cancellationToken)
    {
        var store = await _storeRepository.GetByIdAsync(_currentUser.StoreId, cancellationToken);

        if (store is null)
        {
            return Result<StoreProfileResponse>.NotFound("Store not found.");
        }

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