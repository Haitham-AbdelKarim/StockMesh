namespace Application.DTOs.Auth;

public sealed record RegisterStoreResponse(
    Guid StoreId,
    Guid UserId,
    TokenResponse Tokens);