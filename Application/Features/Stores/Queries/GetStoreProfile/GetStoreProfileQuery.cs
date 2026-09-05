using Application.Common.Models;
using Application.DTOs.Stores;
using MediatR;

namespace Application.Features.Stores.Queries.GetStoreProfile;

public sealed record GetStoreProfileQuery : IRequest<Result<StoreProfileResponse>>;