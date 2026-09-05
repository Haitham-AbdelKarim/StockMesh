using Domain.Enums;

namespace Application.Abstractions.Services;

public interface ICurrentUser
{
    Guid UserId { get; }

    Guid StoreId { get; }

    string Email { get; }

    string Role { get; }

    VerticalCategory VerticalCategory { get; }

    bool IsAuthenticated { get; }
}