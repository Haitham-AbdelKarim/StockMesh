using Application.Common.Models;
using Application.DTOs.Dashboard;
using MediatR;

namespace Application.Features.Dashboard.Queries.GetSlowMovers;

public sealed record GetSlowMoversQuery(
    int Days = 30) : IRequest<Result<IReadOnlyList<SlowMoverResponse>>>;