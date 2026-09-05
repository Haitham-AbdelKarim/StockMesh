namespace Application.DTOs.Auth;

public sealed record JoinStoreResponse(
    Guid UserId,
    string Email);