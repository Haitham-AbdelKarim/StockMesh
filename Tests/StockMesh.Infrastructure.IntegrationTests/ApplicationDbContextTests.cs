using Domain.Entities;
using FluentAssertions;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace StockMesh.Infrastructure.IntegrationTests;

public class ApplicationDbContextTests : IClassFixture<SqlServerContainerFixture>
{
    private readonly SqlServerContainerFixture _fixture;

    public ApplicationDbContextTests(SqlServerContainerFixture fixture)
    {
        _fixture = fixture;
    }

    private static InventoryBatch CreateBatch(Guid storeId, Guid productId, int quantity)
    {
        return new InventoryBatch(
            storeId,
            productId,
            quantity,
            1.0m,
            2.0m,
            5,
            3);
    }

    [Fact]
    public async Task Migrations_ApplyCleanly()
    {
        await using var context = new AppDbContext(_fixture.CreateOptions());

        var tables = (await context.Database
                .SqlQuery<string>($"SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES")
                .ToListAsync())
            .ToHashSet();

        tables.Should().Contain(new[] { "InventoryBatches", "StockMovements", "StockReservations" });
    }

    [Fact]
    public async Task TwoBatches_SameStoreAndProduct_InsertSuccessfully()
    {
        await using var context = new AppDbContext(_fixture.CreateOptions());
        var storeId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        context.InventoryBatches.Add(CreateBatch(storeId, productId, 10));
        context.InventoryBatches.Add(CreateBatch(storeId, productId, 20));
        await context.SaveChangesAsync();

        var result = await context.InventoryBatches
            .Where(b => b.StoreId == storeId && b.ProductId == productId)
            .ToListAsync();

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task ConcurrentUpdates_SameBatch_ThrowConcurrencyException()
    {
        var storeId = Guid.NewGuid();
        var batch = CreateBatch(storeId, Guid.NewGuid(), 100);

        await using (var seed = new AppDbContext(_fixture.CreateOptions()))
        {
            seed.InventoryBatches.Add(batch);
            await seed.SaveChangesAsync();
        }

        await using var contextA = new AppDbContext(_fixture.CreateOptions());
        await using var contextB = new AppDbContext(_fixture.CreateOptions());

        var batchA = await contextA.InventoryBatches.SingleAsync(b => b.Id == batch.Id);
        var batchB = await contextB.InventoryBatches.SingleAsync(b => b.Id == batch.Id);

        batchA.MarkAsShared(10);
        batchB.MarkAsShared(20);

        await contextA.SaveChangesAsync();

        var act = async () => await contextB.SaveChangesAsync();
        await act.Should().ThrowAsync<DbUpdateConcurrencyException>();
    }
}