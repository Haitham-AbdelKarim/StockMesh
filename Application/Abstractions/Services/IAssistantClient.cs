using Application.DTOs.Assistant;

namespace Application.Abstractions.Services;

public interface IAssistantClient
{
    Task StreamAnswerAsync(
        AssistantStreamTicket ticket,
        string bearerToken,
        Stream destination,
        CancellationToken cancellationToken = default);
}