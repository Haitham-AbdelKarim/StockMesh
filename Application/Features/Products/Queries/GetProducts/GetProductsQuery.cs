using Application.Common.Models;
using Application.DTOs.Products;
using Domain.Enums;
using MediatR;

namespace Application.Features.Products.Queries.GetProducts;

public sealed record GetProductsQuery(
    VerticalCategory? VerticalCategory,
    string? Search,
    int Page = 1,
    int PageSize = 20) : IRequest<Result<PaginatedList<ProductResponse>>>;