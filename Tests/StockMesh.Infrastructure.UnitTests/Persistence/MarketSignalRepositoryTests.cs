using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Infrastructure.Persistence;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace StockMesh.Infrastructure.UnitTests.Persistence;

public class MarketSignalRepositoryTests : IDisposable
{
    private readonly AppDbContext _dbContext;
    private readonly DailyMarketSignalRepository _signals;
    private readonly StockReservationRepository _reservations;
    private readonly StockMovementRepository _movements;

    private readonly Guid _productId = Guid.NewGuid();
    private readonly DateTime _day = new(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

    public MarketSignalRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new AppDbContext(options);
        _signals = new DailyMarketSignalRepository(_dbContext);
        _reservations = new StockReservationRepository(_dbContext);
        _movements = new StockMovementRepository(_dbContext);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    [Fact]
    public async Task DailyMarketSignal_AddAndGetByDate_RoundTrips()
    {
        var signal = new DailyMarketSignal(_productId, VerticalCategory.Gaming, _day);
        signal.Update(3, 10, 2);
        await _signals.AddAsync(signal);
        await _signals.SaveChangesAsync();

        var found = await _signals.GetByDateAsync(
            _productId, VerticalCategory.Gaming, _day);

        found.Should().NotBeNull();
        found!.ReservationCount.Should().Be(3);
        found.TransferVolume.Should().Be(10);
        found.ParticipatingStoreCount.Should().Be(2);
    }

    [Fact]
    public async Task DailyMarketSignal_GetByDate_ReturnsNullWhenMissing()
    {
        var found = await _signals.GetByDateAsync(
            Guid.NewGuid(), VerticalCategory.Gaming, _day);

        found.Should().BeNull();
    }

    [Fact]
    public async Task Reservations_GetByDateRange_ReturnsAllStatusesWithinBounds()
    {
        var batchId = Guid.NewGuid();
        var pending = new StockReservation(batchId, Guid.NewGuid(), Guid.NewGuid(), 1, 10m);
        var cancelled = new StockReservation(batchId, Guid.NewGuid(), Guid.NewGuid(), 2, 10m);
        cancelled.Resolve(ReservationStatus.Cancelled);

        _dbContext.StockReservations.AddRange(pending, cancelled);
        await _dbContext.SaveChangesAsync();

        var now = DateTime.UtcNow;
        var rows = await _reservations.GetByDateRangeAsync(
            now.AddHours(-1), now.AddHours(1));

        rows.Should().HaveCount(2);
    }

    [Fact]
    public async Task Reservations_GetByDateRange_ExcludesOutsideBounds()
    {
        var batchId = Guid.NewGuid();
        var reservation = new StockReservation(batchId, Guid.NewGuid(), Guid.NewGuid(), 1, 10m);

        _dbContext.StockReservations.Add(reservation);
        await _dbContext.SaveChangesAsync();

        var rows = await _reservations.GetByDateRangeAsync(
            DateTime.UtcNow.AddHours(1), DateTime.UtcNow.AddHours(2));

        rows.Should().BeEmpty();
    }

    [Fact]
    public async Task Movements_GetByTypeInRange_FiltersTypeAndBounds()
    {
        var batchId = Guid.NewGuid();
        var noon = _day.AddHours(12);
        var transfer = new StockMovement(
            Guid.NewGuid(), batchId, MovementType.NetworkTransferOut, 3, noon, Guid.NewGuid(), 10m, 5m);
        var sale = new StockMovement(
            Guid.NewGuid(), batchId, MovementType.Sale, 1, noon, null, 10m, 5m);
        var staleTransfer = new StockMovement(
            Guid.NewGuid(), batchId, MovementType.NetworkTransferOut, 9, _day.AddDays(-2), Guid.NewGuid(), 10m, 5m);

        _dbContext.StockMovements.AddRange(transfer, sale, staleTransfer);
        await _dbContext.SaveChangesAsync();

        var rows = await _movements.GetByTypeInRangeAsync(
            MovementType.NetworkTransferOut, _day, _day.AddDays(1));

        rows.Should().ContainSingle();
        rows[0].Quantity.Should().Be(3);
    }
}