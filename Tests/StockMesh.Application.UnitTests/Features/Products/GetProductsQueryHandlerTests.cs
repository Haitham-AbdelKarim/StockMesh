using Application.Features.Products.Queries.GetProducts;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using StockMesh.Application.UnitTests.Fakes;

namespace StockMesh.Application.UnitTests.Features.Products;

public class GetProductsQueryHandlerTests
{
    private static Product[] CreateProducts()
    {
        return new[]
        {
            new Product("Zelda: Tears of the Kingdom", VerticalCategory.Gaming, "Nintendo"),
            new Product("PlayStation 5 Console", VerticalCategory.Gaming, "Sony"),
            new Product("Wireless Controller", VerticalCategory.Gaming, "Sony"),
            new Product("Gaming Headset", VerticalCategory.Gaming, "Razer"),
            new Product("Paracetamol 500mg", VerticalCategory.Pharmacy, "Panadol"),
            new Product("Vitamin C 1000mg", VerticalCategory.Pharmacy)
        };
    }

    [Fact]
    public async Task Handle_WithExplicitVertical_ReturnsOnlyThatVertical()
    {
        var handler = CreateHandler(CreateProducts(), VerticalCategory.Electronics);

        var result = await handler.Handle(
            new GetProductsQuery(VerticalCategory.Gaming, null, 1, 20),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(4);
        result.Value.Items.Should().OnlyContain(p => p.VerticalCategory == VerticalCategory.Gaming);
        result.Value.TotalCount.Should().Be(4);
    }

    [Fact]
    public async Task Handle_WithoutVertical_UsesCurrentUsersVertical()
    {
        var handler = CreateHandler(CreateProducts(), VerticalCategory.Gaming);

        var result = await handler.Handle(
            new GetProductsQuery(null, null, 1, 20),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(4);
        result.Value.Items.Should().OnlyContain(p => p.VerticalCategory == VerticalCategory.Gaming);
    }

    [Fact]
    public async Task Handle_WithSearch_FiltersByProductNameWithinVertical()
    {
        var handler = CreateHandler(CreateProducts(), VerticalCategory.Pharmacy);

        var result = await handler.Handle(
            new GetProductsQuery(null, "paracetamol", 1, 20),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().ContainSingle();
        result.Value.Items[0].Name.Should().Be("Paracetamol 500mg");
    }

    [Fact]
    public async Task Handle_WithPaging_ReturnsRequestedSlice()
    {
        var handler = CreateHandler(CreateProducts(), VerticalCategory.Gaming);

        var result = await handler.Handle(
            new GetProductsQuery(null, null, 2, 2),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalCount.Should().Be(4);
        result.Value.Items.Should().HaveCount(2);
        result.Value.Page.Should().Be(2);
        result.Value.TotalPages.Should().Be(2);
    }

    private static GetProductsQueryHandler CreateHandler(
        Product[] products,
        VerticalCategory currentUserVertical)
    {
        return new GetProductsQueryHandler(
            new FakeCurrentUser { VerticalCategory = currentUserVertical },
            new FakeProductRepository(products));
    }
}