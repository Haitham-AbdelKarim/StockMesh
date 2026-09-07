using Application.Features.Expenses.Queries.GetExpenses;
using Domain.Entities;
using FluentAssertions;
using StockMesh.Application.UnitTests.Fakes;

namespace StockMesh.Application.UnitTests.Features.Expenses;

public class GetExpensesQueryHandlerTests
{
    private static readonly Guid StoreId = Guid.NewGuid();

    [Fact]
    public async Task Handle_ReturnsPageOrderedByIncurredAtDescending()
    {
        var expenses = new FakeExpenseRepository(
            new Expense(StoreId, "Rent", 100m, new DateTime(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc)),
            new Expense(StoreId, "Utilities", 50m, new DateTime(2026, 6, 3, 12, 0, 0, DateTimeKind.Utc)));
        var handler = new GetExpensesQueryHandler(
            new FakeCurrentUser { StoreId = StoreId },
            expenses);

        var result = await handler.Handle(new GetExpensesQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(2);
        result.Value.TotalCount.Should().Be(2);
        result.Value.Items[0].Category.Should().Be("Utilities");
        result.Value.Items[1].Category.Should().Be("Rent");
    }

    [Fact]
    public async Task Handle_WithCategoryFilter_ReturnsOnlyMatching()
    {
        var expenses = new FakeExpenseRepository(
            new Expense(StoreId, "Rent", 100m, new DateTime(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc)),
            new Expense(StoreId, "Utilities", 50m, new DateTime(2026, 6, 3, 12, 0, 0, DateTimeKind.Utc)));
        var handler = new GetExpensesQueryHandler(
            new FakeCurrentUser { StoreId = StoreId },
            expenses);

        var result = await handler.Handle(
            new GetExpensesQuery(Category: "Rent"),
            CancellationToken.None);

        result.Value!.Items.Should().ContainSingle().Which.Category.Should().Be("Rent");
        result.Value.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_RespectsPageAndPageSize()
    {
        var expenses = new FakeExpenseRepository(
            new Expense(StoreId, "A", 1m, new DateTime(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc)),
            new Expense(StoreId, "B", 2m, new DateTime(2026, 6, 2, 12, 0, 0, DateTimeKind.Utc)),
            new Expense(StoreId, "C", 3m, new DateTime(2026, 6, 3, 12, 0, 0, DateTimeKind.Utc)));
        var handler = new GetExpensesQueryHandler(
            new FakeCurrentUser { StoreId = StoreId },
            expenses);

        var result = await handler.Handle(
            new GetExpensesQuery(Page: 2, PageSize: 2),
            CancellationToken.None);

        result.Value!.Items.Should().ContainSingle().Which.Category.Should().Be("A");
        result.Value.TotalCount.Should().Be(3);
    }

    [Fact]
    public void Validate_WithOutOfRangePaging_ReturnsErrors()
    {
        var validator = new GetExpensesQueryValidator();

        var result = validator.Validate(new GetExpensesQuery(Page: 0, PageSize: 0));

        result.IsValid.Should().BeFalse();
    }
}