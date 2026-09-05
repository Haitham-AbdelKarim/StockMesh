using Application.Abstractions.Repositories;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Infrastructure.Persistence;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace StockMesh.Infrastructure.UnitTests.Persistence;

public class ProductRepositoryTests : IDisposable
{
    private readonly AppDbContext _dbContext;
    private readonly IProductRepository _repository;

    public ProductRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new AppDbContext(options);
        _repository = new ProductRepository(_dbContext);

        _dbContext.Products.AddRange(
            new Product("PlayStation 5 Console", VerticalCategory.Gaming, "Sony"),
            new Product("Xbox Series X Console", VerticalCategory.Gaming, "Microsoft"),
            new Product("Paracetamol 500mg", VerticalCategory.Pharmacy, "Panadol"),
            new Product("Vitamin C 1000mg", VerticalCategory.Pharmacy));

        _dbContext.SaveChanges();
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    [Fact]
    public async Task GetAsync_WithNoFilters_ReturnsAll()
    {
        var (items, totalCount) = await _repository.GetAsync(null, null, 1, 20);

        totalCount.Should().Be(4);
        items.Should().HaveCount(4);
    }

    [Fact]
    public async Task GetAsync_FiltersByVerticalCategory()
    {
        var (items, totalCount) = await _repository.GetAsync(VerticalCategory.Pharmacy, null, 1, 20);

        totalCount.Should().Be(2);
        items.Should().OnlyContain(p => p.VerticalCategory == VerticalCategory.Pharmacy);
    }

    [Fact]
    public async Task GetAsync_SearchMatchesProductNameCaseInsensitively()
    {
        var (items, totalCount) = await _repository.GetAsync(null, "paracetamol", 1, 20);

        totalCount.Should().Be(1);
        items.Should().ContainSingle(p => p.Name == "Paracetamol 500mg");
    }

    [Fact]
    public async Task GetAsync_SearchMatchesBrand()
    {
        var (items, totalCount) = await _repository.GetAsync(null, "panadol", 1, 20);

        totalCount.Should().Be(1);
        items.Should().ContainSingle();
    }

    [Fact]
    public async Task GetAsync_ReturnsResultOrderedByName()
    {
        var (items, _) = await _repository.GetAsync(null, null, 1, 20);

        items.Should().BeInAscendingOrder(p => p.Name);
    }

    [Fact]
    public async Task GetAsync_PaginationReturnsRequestedSlice()
    {
        var (items, totalCount) = await _repository.GetAsync(null, null, 2, 2);

        totalCount.Should().Be(4);
        items.Should().HaveCount(2);
    }
}