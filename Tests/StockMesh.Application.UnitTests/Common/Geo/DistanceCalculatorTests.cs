using Application.Common.Geo;
using FluentAssertions;

namespace StockMesh.Application.UnitTests.Common.Geo;

public class DistanceCalculatorTests
{
    [Fact]
    public void HaversineKm_SamePoint_ReturnsZero()
    {
        var distance = DistanceCalculator.HaversineKm(30.0444, 31.2357, 30.0444, 31.2357);

        distance.Should().BeApproximately(0, 0.0001);
    }

    [Fact]
    public void HaversineKm_CairoToGiza_ReturnsExpectedDistance()
    {
        var distance = DistanceCalculator.HaversineKm(
            30.0444196, 31.2357116, 30.0130559, 31.2088537);

        distance.Should().BeApproximately(4.27, 0.5);
    }

    [Fact]
    public void HaversineKm_LondonToParis_ReturnsExpectedDistance()
    {
        var distance = DistanceCalculator.HaversineKm(
            51.5074, -0.1278, 48.8566, 2.3522);

        distance.Should().BeApproximately(343.5, 5);
    }

    [Fact]
    public void HaversineKm_IsSymmetric()
    {
        var forward = DistanceCalculator.HaversineKm(30.0444, 31.2357, 30.0131, 31.2089);
        var backward = DistanceCalculator.HaversineKm(30.0131, 31.2089, 30.0444, 31.2357);

        forward.Should().BeApproximately(backward, 0.001);
    }
}