using Application.Abstractions.Repositories;
using Application.Abstractions.Services;
using Application.Common.Models;
using Application.DTOs.Products;
using Domain.Entities;
using MediatR;

namespace Application.Features.Products.Queries.GetProducts;

public sealed class GetProductsQueryHandler :
    IRequestHandler<GetProductsQuery, Result<PaginatedList<ProductResponse>>>
{
    private readonly ICurrentUser _currentUser;
    private readonly IProductRepository _productRepository;

    public GetProductsQueryHandler(
        ICurrentUser currentUser,
        IProductRepository productRepository)
    {
        _currentUser = currentUser;
        _productRepository = productRepository;
    }

    public async Task<Result<PaginatedList<ProductResponse>>> Handle(
        GetProductsQuery query,
        CancellationToken cancellationToken)
    {
        var verticalCategory = query.VerticalCategory ?? _currentUser.VerticalCategory;

        var (items, totalCount) = await _productRepository.GetAsync(
            verticalCategory,
            query.Search,
            query.Page,
            query.PageSize,
            cancellationToken);

        var paginated = PaginatedList<ProductResponse>.Create(
            items.Select(ToResponse).ToList(),
            totalCount,
            query.Page,
            query.PageSize);

        return Result<PaginatedList<ProductResponse>>.Success(paginated);
    }

    private static ProductResponse ToResponse(Product product)
    {
        return new ProductResponse(
            product.Id,
            product.Name,
            product.VerticalCategory,
            product.Brand,
            product.Barcode);
    }
}