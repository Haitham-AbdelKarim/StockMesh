using Domain.Entities;
using Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Identity;

public class StoreUser : IdentityUser<Guid>
{
    public Guid StoreId { get; set; }

    public VerticalCategory VerticalCategory { get; set; }

    public string? RefreshTokenHash { get; set; }

    public DateTimeOffset? RefreshTokenExpiresAt { get; set; }

    public DateTimeOffset? RefreshTokenCreatedAt { get; set; }

    public Store Store { get; set; } = null!;
}