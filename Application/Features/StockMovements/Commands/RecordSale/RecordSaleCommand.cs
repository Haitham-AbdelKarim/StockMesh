using Application.Common.Models;
using Application.DTOs.StockMovements;
using MediatR;

namespace Application.Features.StockMovements.Commands.RecordSale;

public sealed record RecordSaleCommand(
    Guid BatchId,
    int Quantity) : IRequest<Result<SaleResponse>>;