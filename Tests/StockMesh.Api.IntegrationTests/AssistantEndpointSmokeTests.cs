using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;

namespace StockMesh.Api.IntegrationTests;

public class AssistantEndpointSmokeTests : IClassFixture<TestApiFactory>
{
    private readonly TestApiFactory _factory;

    public AssistantEndpointSmokeTests(TestApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Ask_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/v1/assistant/ask",
            new { question = "How can I increase profit?" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Ask_WithEmptyQuestion_ReturnsUnprocessableEntity()
    {
        var client = AuthenticatedClient();

        var response = await client.PostAsJsonAsync(
            "/api/v1/assistant/ask",
            new { question = "" });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task GetConversations_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/assistant/conversations");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AgentLogs_WithoutInternalKey_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/v1/agent-logs",
            new
            {
                storeId = Guid.NewGuid(),
                userId = Guid.NewGuid(),
                conversationId = Guid.NewGuid(),
                question = "Q?",
                toolCalls = "[]",
                finalAnswer = "A.",
            });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AgentLogs_WithInternalKeyButInvalidBody_ReturnsUnprocessableEntity()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Agent-Internal-Key", "test-internal-key");

        var response = await client.PostAsJsonAsync(
            "/api/v1/agent-logs",
            new
            {
                storeId = Guid.NewGuid(),
                userId = Guid.NewGuid(),
                conversationId = Guid.NewGuid(),
                question = "",
                toolCalls = "[]",
                finalAnswer = "A.",
            });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    private HttpClient AuthenticatedClient()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestAuth.CreateAccessToken(Guid.NewGuid()));

        return client;
    }
}