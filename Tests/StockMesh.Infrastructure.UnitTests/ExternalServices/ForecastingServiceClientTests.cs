using System.Net;
using System.Text;
using System.Text.Json;
using Application.Abstractions.Services;
using FluentAssertions;
using Infrastructure.ExternalServices;
using Microsoft.Extensions.Logging.Abstractions;

namespace StockMesh.Infrastructure.UnitTests.ExternalServices;

public class ForecastingServiceClientTests
{
    private static readonly IReadOnlyList<DailyPoint> Series = new[]
    {
        new DailyPoint(new DateOnly(2026, 1, 1), 3),
        new DailyPoint(new DateOnly(2026, 1, 2), 5)
    };

    [Fact]
    public async Task ForecastAsync_WithSuccessPayload_DeserializesAllFields()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """
                {
                    "insufficient_data": false,
                    "dates": ["2026-01-01", "2026-01-02", "2026-01-03"],
                    "trend": [3.0, 4.0, 5.0],
                    "yhat": [3.1, 4.1, 5.1],
                    "yhat_lower": [2.5, 3.5, 4.5],
                    "yhat_upper": [3.7, 4.7, 5.7]
                }
                """,
                Encoding.UTF8,
                "application/json")
        });
        var client = CreateClient(handler);

        var result = await client.ForecastAsync(Series, 1, CancellationToken.None);

        result.Should().NotBeNull();
        result!.InsufficientData.Should().BeFalse();
        result.Dates.Should().Equal("2026-01-01", "2026-01-02", "2026-01-03");
        result.Trend.Should().Equal(3.0, 4.0, 5.0);
        result.Yhat.Should().Equal(3.1, 4.1, 5.1);
        result.YhatLower.Should().Equal(2.5, 3.5, 4.5);
        result.YhatUpper.Should().Equal(3.7, 4.7, 5.7);
    }

    [Fact]
    public async Task ForecastAsync_SendsSeriesAndHorizonAsSnakeCaseJson()
    {
        HttpRequestMessage? captured = null;
        var handler = new StubHttpMessageHandler(request =>
        {
            captured = request;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"insufficient_data":true,"dates":[],"trend":[],"yhat":[],"yhat_lower":[],"yhat_upper":[]}""",
                    Encoding.UTF8,
                    "application/json")
            };
        });
        var client = CreateClient(handler);

        await client.ForecastAsync(Series, 7, CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.Method.Should().Be(HttpMethod.Post);
        captured.RequestUri!.PathAndQuery.Should().EndWith("/forecast");
        var body = await captured.Content!.ReadAsStringAsync();
        var payload = JsonDocument.Parse(body).RootElement;
        payload.GetProperty("periods_ahead").GetInt32().Should().Be(7);
        var points = payload.GetProperty("series").EnumerateArray().ToList();
        points.Should().HaveCount(2);
        points[0].GetProperty("date").GetString().Should().Be("2026-01-01");
        points[0].GetProperty("value").GetDouble().Should().Be(3);
    }

    [Fact]
    public async Task ForecastAsync_WithInsufficientDataFlag_SurfacesFlag()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """{"insufficient_data":true,"dates":[],"trend":[],"yhat":[],"yhat_lower":[],"yhat_upper":[]}""",
                Encoding.UTF8,
                "application/json")
        });
        var client = CreateClient(handler);

        var result = await client.ForecastAsync(Series, 7, CancellationToken.None);

        result.Should().NotBeNull();
        result!.InsufficientData.Should().BeTrue();
        result.Yhat.Should().BeEmpty();
    }

    [Fact]
    public async Task ForecastAsync_WhenServiceIsDown_ReturnsNullWithoutThrowing()
    {
        var handler = new StubHttpMessageHandler(_ => throw new HttpRequestException("Connection refused."));
        var client = CreateClient(handler);

        var result = await client.ForecastAsync(Series, 7, CancellationToken.None);

        result.Should().BeNull();
        handler.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task ForecastAsync_OnServerError_ReturnsNullWithoutThrowing()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        var client = CreateClient(handler);

        var result = await client.ForecastAsync(Series, 7, CancellationToken.None);

        result.Should().BeNull();
    }

    private static ForecastingServiceClient CreateClient(StubHttpMessageHandler handler)
    {
        var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:8000/") };

        return new ForecastingServiceClient(http, NullLogger<ForecastingServiceClient>.Instance);
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _respond;

        public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> respond)
        {
            _respond = respond;
        }

        public int CallCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            CallCount++;

            return Task.FromResult(_respond(request));
        }
    }
}