using Application.Users.Me;

namespace Application.Users.Login;

public sealed record LoginResponse
{
    public MeResponse Me { get; init; }
    public string AccessToken { get; init; }
    public string RefreshToken { get; init; }
}
