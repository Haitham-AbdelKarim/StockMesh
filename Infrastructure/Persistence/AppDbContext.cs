using Application.Abstractions.Persistence;
using Domain.Entities;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public class AppDbContext : IdentityDbContext<StoreUser, IdentityRole<Guid>, Guid>, IApplicationDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<InventoryBatch> InventoryBatches => Set<InventoryBatch>();

    public DbSet<StockReservation> StockReservations => Set<StockReservation>();

    public DbSet<StockMovement> StockMovements => Set<StockMovement>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public DbSet<Store> Stores => Set<Store>();

    public DbSet<Product> Products => Set<Product>();

    public DbSet<Expense> Expenses => Set<Expense>();

    public DbSet<DailyStoreMetric> DailyStoreMetrics => Set<DailyStoreMetric>();

    public DbSet<DailyProductMetric> DailyProductMetrics => Set<DailyProductMetric>();

    public DbSet<DailyMarketSignal> DailyMarketSignals => Set<DailyMarketSignal>();

    public DbSet<Recommendation> Recommendations => Set<Recommendation>();

    public DbSet<ReservationPayment> ReservationPayments => Set<ReservationPayment>();

    public DbSet<ProcessedStripeEvent> ProcessedStripeEvents => Set<ProcessedStripeEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}