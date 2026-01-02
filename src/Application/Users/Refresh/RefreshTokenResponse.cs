using Application.Users.Me;

namespace Application.Users.Refresh;

public sealed record RefreshTokenResponse
{
    public MeResponse Me { get; init; }
    public string AccessToken { get; init; }
    public string RefreshToken { get; init; }
}
