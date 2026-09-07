using Application.Common.Models;
using Application.DTOs.Dashboard;
using MediatR;

namespace Application.Features.Dashboard.Queries.GetSalesTrend;

public sealed record GetSalesTrendQuery(
    int Days = 7) : IRequest<Result<IReadOnlyList<SalesTrendPointResponse>>>;