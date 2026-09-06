using Application.Common.Models;
using Application.DTOs.Network;
using Domain.Enums;
using MediatR;

namespace Application.Features.Network.Queries.GetNetworkListings;

public sealed record GetNetworkListingsQuery(
    VerticalCategory? Category = null,
    double? MaxDistanceKm = null,
    string? Search = null,
    int Page = 1,
    int PageSize = 20) : IRequest<Result<PaginatedList<NetworkListingResponse>>>;