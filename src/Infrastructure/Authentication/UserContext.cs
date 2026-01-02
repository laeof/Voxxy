using System.Security.Claims;
using Application.Abstractions.Authentication;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.Authentication;

internal sealed class UserContext : IUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserContext(IHttpContextAccessor httpContextAccessor) => _httpContextAccessor = httpContextAccessor;

    private ClaimsPrincipal User =>
        _httpContextAccessor.HttpContext?.User
        ?? throw new ApplicationException("User context is unavailable");

    public Guid UserId => User.GetUserId();

    public string Email => User.GetEmail();

    public string FullName => User.GetFullName();
    
    public string ImageUrl => User.GetImageUrl();

    public string[] UserClaims => User.GetUserClaims();
}
