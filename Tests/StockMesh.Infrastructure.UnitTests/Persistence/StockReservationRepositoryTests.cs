using Application.Abstractions.Repositories;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Infrastructure.Persistence;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace StockMesh.Infrastructure.UnitTests.Persistence;

public class StockReservationRepositoryTests : IDisposable
{
    private readonly AppDbContext _dbContext;
    private readonly IStockReservationRepository _repository;
    private readonly DateTime _now = new(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);
    private readonly Guid _batchId = Guid.NewGuid();
    private readonly Guid _requestingStoreId = Guid.NewGuid();
    private readonly Guid _owningStoreId = Guid.NewGuid();
    private readonly Guid _expiredId;
    private readonly Guid _notExpiredId;

    public StockReservationRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new AppDbContext(options);
        _repository = new StockReservationRepository(_dbContext);

        var expired = new StockReservation(
            _batchId,
            _requestingStoreId,
            _owningStoreId,
            2,
            10m,
            holdExpiresAt: _now.AddMinutes(-1));
        var notExpired = new StockReservation(
            _batchId,
            _requestingStoreId,
            _owningStoreId,
            1,
            12m,
            holdExpiresAt: _now.AddMinutes(1));
        var resolved = new StockReservation(
            _batchId,
            _requestingStoreId,
            _owningStoreId,
            3,
            11m,
            holdExpiresAt: _now.AddMinutes(-1));
        resolved.Resolve(ReservationStatus.Cancelled);

        _expiredId = expired.Id;
        _notExpiredId = notExpired.Id;

        _dbContext.StockReservations.AddRange(expired, notExpired, resolved);
        _dbContext.SaveChanges();
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    [Fact]
    public async Task GetExpiredPendingAsync_ReturnsOnlyExpiredPendingReservations()
    {
        var expired = await _repository.GetExpiredPendingAsync(_now);

        expired.Should().ContainSingle();
        expired[0].Id.Should().Be(_expiredId);
    }

    [Fact]
    public async Task GetExpiredPendingAsync_ExcludesNotYetExpiredPendingReservations()
    {
        var expired = await _repository.GetExpiredPendingAsync(_now);

        expired.Should().NotContain(r => r.Id == _notExpiredId);
    }

    [Fact]
    public async Task GetExpiredPendingAsync_ExcludesResolvedReservationsEvenWhenPastHold()
    {
        var allPendingOrExpired = await _repository.GetExpiredPendingAsync(_now);

        allPendingOrExpired.Should().OnlyContain(r => r.Status == ReservationStatus.Pending);
    }
}