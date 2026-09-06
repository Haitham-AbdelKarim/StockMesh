using Application.Common.Models;
using Application.DTOs.StockMovements;
using Domain.Enums;
using MediatR;

namespace Application.Features.StockMovements.Queries.GetStockMovements;

public sealed record GetStockMovementsQuery(
    MovementType? MovementType = null,
    DateTime? From = null,
    DateTime? To = null,
    Guid? RelatedStoreId = null,
    int Page = 1,
    int PageSize = 20) : IRequest<Result<PaginatedList<StockMovementResponse>>>;