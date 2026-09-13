using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Infrastructure.Persistence;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace StockMesh.Infrastructure.UnitTests.Persistence;

public class RecommendationRepositoryTests : IDisposable
{
    private readonly AppDbContext _dbContext;
    private readonly RecommendationRepository _repository;

    private readonly Guid _storeId = Guid.NewGuid();
    private readonly Guid _productId = Guid.NewGuid();

    public RecommendationRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new AppDbContext(options);
        _repository = new RecommendationRepository(_dbContext);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    [Fact]
    public async Task AddGetAndRemove_RoundTrips()
    {
        var row = new Recommendation(
            _storeId,
            _productId,
            null,
            RecommendedAction.Reorder,
            0.5,
            false,
            new DateTime(2026, 6, 10),
            0.8,
            "prophet-v1",
            "Reorder soon.",
            DateTime.UtcNow);

        await _repository.AddAsync(row);
        await _repository.SaveChangesAsync();

        var stored = await _repository.GetForProductAsync(_storeId, _productId);
        stored.Should().ContainSingle();

        _repository.RemoveRange(stored);
        await _repository.SaveChangesAsync();

        (await _repository.GetForProductAsync(_storeId, _productId)).Should().BeEmpty();
    }

    [Fact]
    public async Task GetForStore_FiltersByActionAndPaginates()
    {
        var now = DateTime.UtcNow;
        await _repository.AddAsync(new Recommendation(
            _storeId, _productId, null, RecommendedAction.Reorder, 0, false, null, 0,
            "prophet-v1", "R1.", now));
        await _repository.AddAsync(new Recommendation(
            _storeId, _productId, null, RecommendedAction.Hold, 0, false, null, 0,
            "prophet-v1", "R2.", now.AddMinutes(1)));
        await _repository.SaveChangesAsync();

        var (items, totalCount) = await _repository.GetForStoreAsync(
            _storeId, RecommendedAction.Hold, 1, 10);

        totalCount.Should().Be(1);
        items.Should().ContainSingle().Which.Reason.Should().Be("R2.");
    }
}