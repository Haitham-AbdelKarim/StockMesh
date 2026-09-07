using Application.DTOs.Expenses;
using Application.Features.Expenses.Commands.RecordExpense;
using Application.Features.Expenses.Queries.GetExpenses;
using Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using StockMesh.Application.UnitTests.Fakes;

namespace StockMesh.Application.UnitTests.Features.Expenses;

public class RecordExpenseCommandHandlerTests
{
    private static readonly Guid StoreId = Guid.NewGuid();
    private static readonly FakeDateTimeProvider Clock = new();

    [Fact]
    public async Task Handle_OnSuccess_RecordsExpenseAndRecomputesMetrics()
    {
        var expenses = new FakeExpenseRepository();
        var materializer = new FakeDailyMetricsMaterializer();
        var unitOfWork = new FakeUnitOfWork();
        var handler = CreateHandler(expenses, materializer, unitOfWork);

        var result = await handler.Handle(
            new RecordExpenseCommand("Rent", 150.5m, Clock.UtcNow, "Monthly rent."),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Should().BeEquivalentTo(new ExpenseResponse(
            result.Value.Id,
            StoreId,
            "Rent",
            150.5m,
            Clock.UtcNow,
            "Monthly rent."));

        expenses.All.Should().ContainSingle();
        expenses.All.Single().StoreId.Should().Be(StoreId);

        materializer.Calls.Should().ContainSingle();
        materializer.Calls.Single().StoreIds.Should().BeEquivalentTo(new[] { StoreId });
        materializer.Calls.Single().Date.Should().Be(Clock.UtcNow.Date);

        unitOfWork.Transactions.Should().ContainSingle().Subject.Committed.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithInvalidCategory_ReturnsErrors()
    {
        var validator = new RecordExpenseCommandValidator();

        var result = validator.Validate(new RecordExpenseCommand(string.Empty, 10m, Clock.UtcNow));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public void Validate_WithNonPositiveAmount_ReturnsErrors()
    {
        var validator = new RecordExpenseCommandValidator();

        var result = validator.Validate(new RecordExpenseCommand("Rent", 0m, Clock.UtcNow));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithValidCommand_IsValid()
    {
        var validator = new RecordExpenseCommandValidator();

        var result = validator.Validate(new RecordExpenseCommand("Rent", 100m, Clock.UtcNow));

        result.IsValid.Should().BeTrue();
    }

    private static RecordExpenseCommandHandler CreateHandler(
        FakeExpenseRepository expenseRepository,
        FakeDailyMetricsMaterializer materializer,
        FakeUnitOfWork unitOfWork)
    {
        return new RecordExpenseCommandHandler(
            new FakeCurrentUser { StoreId = StoreId },
            expenseRepository,
            materializer,
            unitOfWork,
            NullLogger<RecordExpenseCommandHandler>.Instance);
    }
}