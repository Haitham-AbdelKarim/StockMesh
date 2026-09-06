using Application.Common.Models;
using Application.DTOs.StockMovements;
using MediatR;

namespace Application.Features.StockMovements.Commands.RecordRestock;

public sealed record RecordRestockCommand(
    Guid ProductId,
    int Quantity,
    decimal UnitCost,
    string? SupplierName = null,
    DateTime? ExpiryDate = null) : IRequest<Result<RestockResponse>>;