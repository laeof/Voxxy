namespace Application.Users.Me;

public sealed class MeResponse
{
    public Guid Id { get; init; }
    public string Email { get; init; }
    public string FullName { get; init; }
    public string ImageUrl { get; init; }
    public IReadOnlyCollection<string> UserClaims { get; init; }
}