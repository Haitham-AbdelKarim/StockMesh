using Application.Common.Models;
using Application.DTOs.Stores;
using MediatR;

namespace Application.Features.Stores.Commands.UpdateStoreProfile;

public sealed record UpdateStoreProfileCommand(
    string Name,
    double Latitude,
    double Longitude,
    double MaxSearchRadiusKm) : IRequest<Result<StoreProfileResponse>>;