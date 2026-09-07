using Application.Common.Models;
using Application.DTOs.Dashboard;
using MediatR;

namespace Application.Features.Dashboard.Queries.GetLowStock;

public sealed record GetLowStockQuery : IRequest<Result<IReadOnlyList<LowStockItemResponse>>>;