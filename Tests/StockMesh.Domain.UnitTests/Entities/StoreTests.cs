using Domain.Entities;
using Domain.Enums;
using FluentAssertions;

namespace StockMesh.Domain.UnitTests.Entities;

public class StoreTests
{
    [Fact]
    public void Constructor_WithValidInputs_CreatesStore()
    {
        var store = new Store("Downtown Pharmacy", VerticalCategory.Pharmacy, 30.05, 31.25);

        store.Name.Should().Be("Downtown Pharmacy");
        store.VerticalCategory.Should().Be(VerticalCategory.Pharmacy);
        store.Latitude.Should().Be(30.05);
        store.Longitude.Should().Be(31.25);
        store.MaxSearchRadiusKm.Should().Be(30);
        store.IsVerified.Should().BeFalse();
        store.Id.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithMissingName_Throws(string? name)
    {
        var act = () => new Store(name, VerticalCategory.Electronics, 30.05, 31.25);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_WithLatitudeOutOfRange_Throws()
    {
        var act = () => new Store("X", VerticalCategory.Gaming, 91, 31.25);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Constructor_WithLongitudeOutOfRange_Throws()
    {
        var act = () => new Store("X", VerticalCategory.Gaming, 30.05, -181);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Constructor_WithNonPositiveSearchRadius_Throws()
    {
        var act = () => new Store("X", VerticalCategory.Gaming, 30.05, 31.25, 0);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Verify_MarksStoreAsVerified()
    {
        var store = new Store("Downtown", VerticalCategory.Grocery, 30.05, 31.25);

        store.Verify();

        store.IsVerified.Should().BeTrue();
    }
}