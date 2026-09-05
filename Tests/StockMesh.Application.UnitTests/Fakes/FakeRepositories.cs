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