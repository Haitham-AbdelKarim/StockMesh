using Application.Abstractions.Repositories;
using Domain.Entities;
using FluentAssertions;
using Infrastructure.Persistence;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace StockMesh.Infrastructure.UnitTests.Persistence;

public class InventoryBatchRepositoryTests : IDisposable
{
    private readonly AppDbContext _dbContext;
    private readonly IInventoryBatchRepository _repository;
    private readonly Guid _storeId = Guid.NewGuid();
    private readonly Guid _otherStoreId = Guid.NewGuid();
    private readonly Guid _productId = Guid.NewGuid();
    private readonly Guid _otherProductId = Guid.NewGuid();
    private readonly Guid _batch1Id;
    private readonly Guid _batch2Id;

    public InventoryBatchRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new AppDbContext(options);
        _repository = new InventoryBatchRepository(_dbContext);

        var batch1 = new InventoryBatch(
            _storeId, _productId, 10, 5m, 10m, receivedAt: new DateTime(2026, 1, 1));
        batch1.MarkAsShared(2);
        var batch2 = new InventoryBatch(
            _storeId, _productId, 20, 7m, 14m, receivedAt: new DateTime(2026, 2, 1));
        var batch3 = new InventoryBatch(
            _storeId, _otherProductId, 5, 3m, 6m, receivedAt: new DateTime(2026, 1, 15));
        var foreign = new InventoryBatch(
            _otherStoreId, _productId, 99, 1m, 2m, receivedAt: new DateTime(2026, 3, 1));

        _batch1Id = batch1.Id;
        _batch2Id = batch2.Id;

        _dbContext.InventoryBatches.AddRange(batch1, batch2, batch3, foreign);
        _dbContext.SaveChanges();
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    [Fact]
    public async Task GetBatchesAsync_ReturnsOnlyOwnStoresBatches()
    {
        var (items, totalCount) = await _repository.GetBatchesAsync(_storeId, null, 1, 20);

        totalCount.Should().Be(3);
        items.Should().HaveCount(3);
        items.Should().OnlyContain(b => b.StoreId == _storeId);
    }

    [Fact]
    public async Task GetBatchesAsync_ReturnsBatchesOrderedByReceivedAtDescending()
    {
        var (items, _) = await _repository.GetBatchesAsync(_storeId, null, 1, 20);

        items.Should().BeInDescendingOrder(b => b.ReceivedAt);
        items[0].Id.Should().Be(_batch2Id);
    }

    [Fact]
    public async Task GetBatchesAsync_FiltersByProductId()
    {
        var (items, totalCount) = await _repository.GetBatchesAsync(_storeId, _productId, 1, 20);

        totalCount.Should().Be(2);
        items.Should().OnlyContain(b => b.ProductId == _productId);
    }

    [Fact]
    public async Task GetBatchesAsync_Paginates()
    {
        var (items, totalCount) = await _repository.GetBatchesAsync(_storeId, null, 2, 2);

        totalCount.Should().Be(3);
        items.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetLatestByProductAsync_ReturnsMostRecentlyReceivedBatch()
    {
        var latest = await _repository.GetLatestByProductAsync(_storeId, _productId);

        latest.Should().NotBeNull();
        latest!.Id.Should().Be(_batch2Id);
        latest.ReceivedAt.Should().Be(new DateTime(2026, 2, 1));
    }

    [Fact]
    public async Task GetLatestByProductAsync_ReturnsNullWhenStoreHasNoBatch()
    {
        var latest = await _repository.GetLatestByProductAsync(_otherStoreId, _otherProductId);

        latest.Should().BeNull();
    }

    [Fact]
    public async Task TwoBatchesSameProduct_AreIndependentRowsAndEditableById()
    {
        var batch1 = await _repository.GetByIdAsync(_batch1Id);
        batch1!.UnitSalePrice.Should().Be(10m);

        batch1.UpdateDetails(unitSalePrice: 12m);
        _repository.Update(batch1);
        await _repository.SaveChangesAsync();

        var reloadedBatch1 = await _repository.GetByIdAsync(_batch1Id);
        var reloadedBatch2 = await _repository.GetByIdAsync(_batch2Id);

        reloadedBatch1!.UnitSalePrice.Should().Be(12m);
        reloadedBatch2!.UnitSalePrice.Should().Be(14m);
        reloadedBatch2.QuantityRemaining.Should().Be(20);
    }

    [Fact]
    public async Task GetProductStockSummariesAsync_SumsAcrossAllBatchesPerProduct()
    {
        var summaries = await _repository.GetProductStockSummariesAsync(_storeId);

        summaries.Should().HaveCount(2);

        var productSummary = summaries.Single(s => s.ProductId == _productId);
        productSummary.TotalQuantityRemaining.Should().Be(28);
        productSummary.TotalSharedQuantity.Should().Be(2);
        productSummary.BatchCount.Should().Be(2);
    }

    [Fact]
    public async Task GetSharedByStoresAsync_ReturnsOnlySharedBatchesForGivenStores()
    {
        var batches = await _repository.GetSharedByStoresAsync(new[] { _storeId, _otherStoreId });

        batches.Should().ContainSingle();
        batches[0].Id.Should().Be(_batch1Id);
        batches[0].SharedQuantity.Should().Be(2);
    }

    [Fact]
    public async Task GetSharedByStoresAsync_ExcludesPrivateOnlyBatches()
    {
        var batches = await _repository.GetSharedByStoresAsync(new[] { _storeId });

        batches.Should().ContainSingle();
        batches[0].Id.Should().Be(_batch1Id);
    }
}