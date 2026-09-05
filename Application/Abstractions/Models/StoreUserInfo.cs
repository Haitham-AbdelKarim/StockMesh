using Domain.Enums;

namespace Application.Abstractions.Models;

public sealed record StoreUserInfo(
    Guid UserId,
    Guid StoreId,
    string Email,
    string Role,
    VerticalCategory VerticalCategory);