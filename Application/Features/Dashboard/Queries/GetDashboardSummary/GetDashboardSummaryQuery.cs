using Application.Common.Models;
using Application.DTOs.Dashboard;
using MediatR;

namespace Application.Features.Dashboard.Queries.GetDashboardSummary;

public sealed record GetDashboardSummaryQuery(
    DateTime? Date = null) : IRequest<Result<DashboardSummaryResponse>>;