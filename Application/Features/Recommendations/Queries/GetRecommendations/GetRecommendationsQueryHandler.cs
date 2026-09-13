using Application.Abstractions.Repositories;
using Application.Abstractions.Services;
using Application.Common.Models;
using Application.DTOs.Recommendations;
using MediatR;

namespace Application.Features.Recommendations.Queries.GetRecommendations;

public sealed class GetRecommendationsQueryHandler :
    IRequestHandler<GetRecommendationsQuery, Result<PaginatedList<RecommendationResponse>>>
{
    private readonly ICurrentUser _currentUser;
    private readonly IRecommendationRepository _recommendationRepository;
    private readonly IProductRepository _productRepository;

    public GetRecommendationsQueryHandler(
        ICurrentUser currentUser,
        IRecommendationRepository recommendationRepository,
        IProductRepository productRepository)
    {
        _currentUser = currentUser;
        _recommendationRepository = recommendationRepository;
        _productRepository = productRepository;
    }

    public async Task<Result<PaginatedList<RecommendationResponse>>> Handle(
        GetRecommendationsQuery query,
        CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _recommendationRepository.GetForStoreAsync(
            _currentUser.StoreId,
            query.RecommendedAction,
            query.Page,
            query.PageSize,
            cancellationToken);

        var products = await _productRepository.GetByIdsAsync(
            items.Select(r => r.ProductId).Distinct().ToList(),
            cancellationToken);
        var namesById = products.ToDictionary(p => p.Id, p => p.Name);

        var responses = items
            .Select(r => RecommendationMapper.ToResponse(
                r,
                namesById.GetValueOrDefault(r.ProductId, string.Empty)))
            .ToList();

        return Result<PaginatedList<RecommendationResponse>>.Success(
            PaginatedList<RecommendationResponse>.Create(responses, totalCount, query.Page, query.PageSize));
    }
}