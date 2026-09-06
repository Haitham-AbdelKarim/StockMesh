using Application.Abstractions.Repositories;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Infrastructure.Persistence;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace StockMesh.Infrastructure.UnitTests.Persistence;

public class StoreRepositoryTests : IDisposable
{
    private readonly AppDbContext _dbContext;
    private readonly IStoreRepository _repository;

    public StoreRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new AppDbContext(options);
        _repository = new StoreRepository(_dbContext);

        _dbContext.Stores.AddRange(
            new Store("Pharmacy A", VerticalCategory.Pharmacy, 30.04, 31.23),
            new Store("Pharmacy B", VerticalCategory.Pharmacy, 30.02, 31.21),
            new Store("Gaming Store", VerticalCategory.Gaming, 30.04, 31.23));

        _dbContext.SaveChanges();
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    [Fact]
    public async Task GetByVerticalAsync_ReturnsOnlyStoresInThatVertical()
    {
        var stores = await _repository.GetByVerticalAsync(VerticalCategory.Pharmacy);

        stores.Should().HaveCount(2);
        stores.Should().OnlyContain(s => s.VerticalCategory == VerticalCategory.Pharmacy);
    }
}