using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Application.Abstractions.Services;
using Application.DTOs.Assistant;
using Infrastructure.ExternalServices;
using Microsoft.Extensions.Logging;

namespace Infrastructure.ExternalServices;

public sealed class AssistantServiceClient : IAssistantClient
{
    private readonly HttpClient _httpClient;
    private readonly AssistantSettings _settings;
    private readonly ILogger<AssistantServiceClient> _logger;

    public AssistantServiceClient(
        HttpClient httpClient,
        AssistantSettings settings,
        ILogger<AssistantServiceClient> logger)
    {
        _httpClient = httpClient;
        _settings = settings;
        _logger = logger;
    }

    public async Task StreamAnswerAsync(
        AssistantStreamTicket ticket,
        string bearerToken,
        Stream destination,
        CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.Serialize(new
        {
            question = ticket.Question,
            conversation_id = ticket.ConversationId.ToString("N"),
            store_id = ticket.StoreId,
            user_id = ticket.UserId,
            auth_token = bearerToken.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                ? bearerToken["Bearer ".Length..]
                : bearerToken,
            history = ticket.History.Select(turn => new
            {
                question = turn.Question,
                answer = turn.Answer,
            }),
        });

        using var request = new HttpRequestMessage(HttpMethod.Post, "ask")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json"),
        };

        using var agentResponse = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        agentResponse.EnsureSuccessStatusCode();

        await using var agentStream = await agentResponse.Content.ReadAsStreamAsync(cancellationToken);

        var buffer = new byte[8192];
        int bytesRead;

        while ((bytesRead = await agentStream.ReadAsync(buffer, cancellationToken)) > 0)
        {
            await destination.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
            await destination.FlushAsync(cancellationToken);
        }
    }
}