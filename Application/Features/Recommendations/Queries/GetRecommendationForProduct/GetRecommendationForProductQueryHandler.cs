using Application.Abstractions.Repositories;
using Application.Abstractions.Services;
using Application.Common.Models;
using Application.DTOs.Recommendations;
using MediatR;

namespace Application.Features.Recommendations.Queries.GetRecommendationForProduct;

public sealed class GetRecommendationForProductQueryHandler :
    IRequestHandler<GetRecommendationForProductQuery, Result<RecommendationResponse>>
{
    private readonly ICurrentUser _currentUser;
    private readonly IRecommendationRepository _recommendationRepository;
    private readonly IProductRepository _productRepository;

    public GetRecommendationForProductQueryHandler(
        ICurrentUser currentUser,
        IRecommendationRepository recommendationRepository,
        IProductRepository productRepository)
    {
        _currentUser = currentUser;
        _recommendationRepository = recommendationRepository;
        _productRepository = productRepository;
    }

    public async Task<Result<RecommendationResponse>> Handle(
        GetRecommendationForProductQuery query,
        CancellationToken cancellationToken)
    {
        var rows = await _recommendationRepository.GetForProductAsync(
            _currentUser.StoreId,
            query.ProductId,
            cancellationToken);

        var latest = rows
            .Where(r => r.BatchId is null)
            .OrderByDescending(r => r.GeneratedAt)
            .FirstOrDefault();

        if (latest is null)
        {
            return Result<RecommendationResponse>.NotFound("No recommendation found for this product.");
        }

        var product = await _productRepository.GetByIdAsync(query.ProductId, cancellationToken);

        return Result<RecommendationResponse>.Success(
            RecommendationMapper.ToResponse(latest, product?.Name ?? string.Empty));
    }
}