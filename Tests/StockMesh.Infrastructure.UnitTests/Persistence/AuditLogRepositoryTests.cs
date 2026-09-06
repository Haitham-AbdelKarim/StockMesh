using Application.Abstractions.Repositories;
using Domain.Entities;
using FluentAssertions;
using Infrastructure.Persistence;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace StockMesh.Infrastructure.UnitTests.Persistence;

public class AuditLogRepositoryTests : IDisposable
{
    private readonly AppDbContext _dbContext;
    private readonly IAuditLogRepository _repository;

    public AuditLogRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new AppDbContext(options);
        _repository = new AuditLogRepository(_dbContext);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    [Fact]
    public async Task AddAsync_ThenSave_PersistsEntry()
    {
        var entry = new AuditLog(
            "StockReservation",
            Guid.NewGuid(),
            "reservation.created",
            Guid.NewGuid());

        await _repository.AddAsync(entry);
        await _repository.SaveChangesAsync();

        var persisted = await _dbContext.AuditLogs.FirstOrDefaultAsync(a => a.Id == entry.Id);

        persisted.Should().NotBeNull();
        persisted!.EntityType.Should().Be("StockReservation");
        persisted.Action.Should().Be("reservation.created");
    }
}