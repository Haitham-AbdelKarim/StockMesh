using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Abstractions.Persistence;

public interface IApplicationDbContext
{
    DbSet<InventoryBatch> InventoryBatches { get; }

    DbSet<StockReservation> StockReservations { get; }

    DbSet<StockMovement> StockMovements { get; }

    DbSet<AuditLog> AuditLogs { get; }

    DbSet<Expense> Expenses { get; }

    DbSet<DailyStoreMetric> DailyStoreMetrics { get; }

    DbSet<DailyProductMetric> DailyProductMetrics { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}