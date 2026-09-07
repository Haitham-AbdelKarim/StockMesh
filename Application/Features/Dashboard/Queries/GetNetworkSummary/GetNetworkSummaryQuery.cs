using Application.Common.Models;
using Application.DTOs.Dashboard;
using MediatR;

namespace Application.Features.Dashboard.Queries.GetNetworkSummary;

public sealed record GetNetworkSummaryQuery(
    int Days = 30) : IRequest<Result<NetworkSummaryResponse>>;