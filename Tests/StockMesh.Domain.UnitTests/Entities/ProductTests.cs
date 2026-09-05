using Domain.Entities;
using Domain.Enums;
using FluentAssertions;

namespace StockMesh.Domain.UnitTests.Entities;

public class ProductTests
{
    [Fact]
    public void Constructor_WithValidInputs_CreatesProduct()
    {
        var product = new Product("PlayStation 5 Console", VerticalCategory.Gaming, "Sony", "711719541028");

        product.Name.Should().Be("PlayStation 5 Console");
        product.VerticalCategory.Should().Be(VerticalCategory.Gaming);
        product.Brand.Should().Be("Sony");
        product.Barcode.Should().Be("711719541028");
        product.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void Constructor_WithoutBrandOrBarcode_CreatesProduct()
    {
        var product = new Product("Paracetamol 500mg", VerticalCategory.Pharmacy);

        product.Brand.Should().BeNull();
        product.Barcode.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithMissingName_Throws(string? name)
    {
        var act = () => new Product(name!, VerticalCategory.Grocery);

        act.Should().Throw<ArgumentException>();
    }
}