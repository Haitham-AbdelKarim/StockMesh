using Application.Common.Models;
using FluentAssertions;

namespace StockMesh.Application.UnitTests;

public class PaginatedListTests
{
    [Fact]
    public void Create_ComputesTotalPages()
    {
        var list = PaginatedList<int>.Create([1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15], 15, 1, 10);

        list.TotalPages.Should().Be(2);
        list.HasPreviousPage.Should().BeFalse();
        list.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public void LastPage_HasPrevious_NoNext()
    {
        var list = PaginatedList<int>.Create([11, 12, 13, 14, 15], 15, 2, 10);

        list.TotalPages.Should().Be(2);
        list.HasPreviousPage.Should().BeTrue();
        list.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public void EmptySet_TotalPagesZero_NoNext()
    {
        var list = PaginatedList<int>.Create([], 0, 1, 10);

        list.TotalPages.Should().Be(0);
        list.HasPreviousPage.Should().BeFalse();
        list.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public void ClampsPageAndPageSizeToAtLeastOne()
    {
        var list = PaginatedList<int>.Create([], 0, 0, 0);

        list.Page.Should().Be(1);
        list.PageSize.Should().Be(1);
    }

    [Fact]
    public void ExactMultiple_TotalPagesIsQuotient()
    {
        var list = PaginatedList<int>.Create([.. Enumerable.Range(1, 30)], 30, 3, 10);

        list.TotalPages.Should().Be(3);
        list.HasNextPage.Should().BeFalse();
    }
}