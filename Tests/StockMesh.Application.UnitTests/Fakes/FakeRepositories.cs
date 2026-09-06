using Application.Abstractions.Models;
using Application.Abstractions.Repositories;
using Application.Abstractions.Services;
using Domain.Entities;
using Domain.Enums;

namespace StockMesh.Application.UnitTests.Fakes;

internal sealed class FakeStoreRepository : IStoreRepository
{
    private readonly Dictionary<Guid, Store> _stores = new();

    public FakeStoreRepository(params Store[] stores)
    {
        foreach (var store in stores)
        {
            _stores[store.Id] = store;
        }
    }

    public Task<Store?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_stores.GetValueOrDefault(id));
    }

    public Task<Guid> AddAsync(Store store, CancellationToken cancellationToken = default)
    {
        _stores[store.Id] = store;
        return Task.FromResult(store.Id);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(0);
    }
}

internal sealed class FakeProductRepository : IProductRepository
{
    private readonly List<Product> _products;

    public FakeProductRepository(params Product[] products)
    {
        _products = products.ToList();
    }

    public Task<(IReadOnlyList<Product> Items, int TotalCount)> GetAsync(
        VerticalCategory? verticalCategory,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _products.AsEnumerable();

        if (verticalCategory is { } category)
        {
            query = query.Where(p => p.VerticalCategory == category);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(p =>
                p.Name.ToLower().Contains(term)
                || (p.Brand != null && p.Brand.ToLower().Contains(term)));
        }

        var all = query
            .OrderBy(p => p.Name)
            .ToList();

        var items = all
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return Task.FromResult(((IReadOnlyList<Product>)items, all.Count));
    }

    public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_products.FirstOrDefault(p => p.Id == id));
    }

    public Task<IReadOnlyList<Product>> GetByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<Product>>(
            _products.Where(p => ids.Contains(p.Id)).ToList());
    }
}

internal sealed class FakeInventoryBatchRepository : IInventoryBatchRepository
{
    private readonly Dictionary<Guid, InventoryBatch> _batches = new();

    public FakeInventoryBatchRepository(params InventoryBatch[] batches)
    {
        foreach (var batch in batches)
        {
            _batches[batch.Id] = batch;
        }
    }

    public int Count
    {
        get { return _batches.Count; }
    }

    public Task<InventoryBatch?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_batches.GetValueOrDefault(id));
    }

    public Task<IReadOnlyList<InventoryBatch>> GetByProductAsync(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<InventoryBatch>>(
            _batches.Values.Where(b => b.ProductId == productId).ToList());
    }

    public Task<InventoryBatch?> GetLatestByProductAsync(
        Guid storeId,
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(
            _batches.Values
                .Where(b => b.StoreId == storeId && b.ProductId == productId)
                .OrderByDescending(b => b.ReceivedAt)
                .FirstOrDefault());
    }

    public Task<(IReadOnlyList<InventoryBatch> Items, int TotalCount)> GetBatchesAsync(
        Guid storeId,
        Guid? productId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _batches.Values
            .Where(b => b.StoreId == storeId);

        if (productId is { } id)
        {
            query = query.Where(b => b.ProductId == id);
        }

        var all = query
            .OrderByDescending(b => b.ReceivedAt)
            .ToList();

        var items = all
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return Task.FromResult(((IReadOnlyList<InventoryBatch>)items, all.Count));
    }

    public Task<IReadOnlyList<ProductStockSummary>> GetProductStockSummariesAsync(
        Guid storeId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<ProductStockSummary>>(
            _batches.Values
                .Where(b => b.StoreId == storeId)
                .GroupBy(b => b.ProductId)
                .Select(g => new ProductStockSummary(
                    g.Key,
                    g.Sum(b => b.QuantityRemaining),
                    g.Sum(b => b.SharedQuantity),
                    g.Count()))
                .OrderBy(s => s.ProductId)
                .ToList());
    }

    public Task<Guid> AddAsync(InventoryBatch batch, CancellationToken cancellationToken = default)
    {
        _batches[batch.Id] = batch;
        return Task.FromResult(batch.Id);
    }

    public void Update(InventoryBatch batch)
    {
        _batches[batch.Id] = batch;
    }

    public void Remove(InventoryBatch batch)
    {
        _batches.Remove(batch.Id);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(0);
    }
}

internal sealed class FakeCurrentUser : ICurrentUser
{
    public Guid UserId { get; init; }

    public Guid StoreId { get; init; }

    public string Email { get; init; } = string.Empty;

    public string Role { get; init; } = string.Empty;

    public VerticalCategory VerticalCategory { get; init; }

    public bool IsAuthenticated { get; init; } = true;
}