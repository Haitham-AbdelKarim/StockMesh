using Application.Abstractions.Options;
using Application.Common.Models;
using Application.Features.Reservations.Commands.ReserveStock;
using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using StockMesh.Application.UnitTests.Fakes;

namespace StockMesh.Application.UnitTests.Features.Reservations;

public class ReservationConcurrencyTests
{
    private static readonly Guid OwnerStoreId = Guid.NewGuid();
    private static readonly Guid RequesterStoreId = Guid.NewGuid();

    private static readonly Product Product =
        new("Paracetamol 500mg", VerticalCategory.Pharmacy, "Panadol");

    private static readonly FakeDateTimeProvider Clock = new();

    [Fact]
    public async Task ConcurrentReservations_WhenSerializedByLock_NeverOverConsumeOrGoNegative()
    {
        var batch = CreateSharedBatch(5);
        var unitOfWork = new FakeUnitOfWork();
        var reservationRepository = new FakeStockReservationRepository();
        var handler = CreateHandler(
            new FakeReservationLockService(acquireResult: true, serialize: true),
            new FakeInventoryBatchRepository(batch),
            reservationRepository,
            new FakeAuditLogRepository(),
            unitOfWork);

        var tasks = Enumerable.Range(0, 20)
            .Select(_ => handler.Handle(new ReserveStockCommand(batch.Id, 1), CancellationToken.None))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        results.Count(r => r.IsSuccess).Should().Be(5);
        results.Count(r => r.Kind == FailureKind.Conflict).Should().Be(15);
        batch.SharedQuantity.Should().Be(0);
        reservationRepository.All.Should().HaveCount(5);
        reservationRepository.All.Sum(r => r.Quantity).Should().Be(5);
        reservationRepository.All.Should().OnlyContain(r => r.Status == ReservationStatus.Pending);
        unitOfWork.Transactions.Count(t => t.Committed).Should().Be(5);
    }

    [Fact]
    public void ReserveFromSharedPool_UnderUnserializedConcurrentCalls_IsNeverNegativeOrAboveInitial()
    {
        for (var round = 0; round < 20; round++)
        {
            var batch = CreateSharedBatch(5);

            Parallel.For(0, 100, _ =>
            {
                try
                {
                    batch.ReserveFromSharedPool(1);
                }
                catch (InsufficientStockException)
                {
                }
            });

            batch.SharedQuantity.Should().BeGreaterThanOrEqualTo(0);
            batch.SharedQuantity.Should().BeLessThanOrEqualTo(5);
        }
    }

    private static InventoryBatch CreateSharedBatch(int sharedQuantity)
    {
        var batch = new InventoryBatch(OwnerStoreId, Product.Id, 20, 5m, 10m);
        batch.MarkAsShared(sharedQuantity);

        return batch;
    }

    private static ReserveStockCommandHandler CreateHandler(
        FakeReservationLockService lockService,
        FakeInventoryBatchRepository batchRepository,
        FakeStockReservationRepository reservationRepository,
        FakeAuditLogRepository auditRepository,
        FakeUnitOfWork unitOfWork)
    {
        return new ReserveStockCommandHandler(
            new FakeCurrentUser { StoreId = RequesterStoreId },
            Clock,
            batchRepository,
            reservationRepository,
            auditRepository,
            lockService,
            unitOfWork,
            new ReservationOptions(),
            NullLogger<ReserveStockCommandHandler>.Instance);
    }
}