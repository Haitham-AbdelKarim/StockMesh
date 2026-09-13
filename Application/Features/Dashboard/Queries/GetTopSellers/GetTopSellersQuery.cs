using Application.Common.Models;
using Application.DTOs.Dashboard;
using MediatR;

namespace Application.Features.Dashboard.Queries.GetTopSellers;

public sealed record GetTopSellersQuery(
    int TopN = 5,
    DateTime? From = null,
    DateTime? To = null) : IRequest<Result<IReadOnlyList<TopSellerResponse>>>;