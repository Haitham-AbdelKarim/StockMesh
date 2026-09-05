using System.Security.Claims;
using Application.Abstractions.Services;
using Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.Services;

public sealed class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid UserId => GetGuidClaim(ClaimsHelper.NameIdentifier, JwtClaimNames.Sub);

    public Guid StoreId => GetGuidClaim("store_id");

    public string Email => GetStringClaim(ClaimsHelper.Email, JwtClaimNames.Email) ?? string.Empty;

    public string Role => GetStringClaim(ClaimsHelper.Role, "role") ?? string.Empty;

    public VerticalCategory VerticalCategory =>
        Enum.TryParse<VerticalCategory>(GetStringClaim("vertical_category"), out var category)
            ? category
            : VerticalCategory.Other;

    public bool IsAuthenticated =>
        _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated == true;

    private Guid GetGuidClaim(params string[] claimTypes)
    {
        var value = GetStringClaim(claimTypes);
        return Guid.TryParse(value, out var id) ? id : Guid.Empty;
    }

    private string? GetStringClaim(params string[] claimTypes)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user is null)
        {
            return null;
        }

        foreach (var claimType in claimTypes)
        {
            var claim = user.FindFirst(claimType);
            if (claim is not null)
            {
                return claim.Value;
            }
        }

        return null;
    }

    private static class JwtClaimNames
    {
        public const string Sub = "sub";
        public const string Email = "email";
    }

    private static class ClaimsHelper
    {
        public const string NameIdentifier = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier";
        public const string Email = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress";
        public const string Role = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";
    }
}