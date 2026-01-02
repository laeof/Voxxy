namespace Application.Users.GetByEmail;

public sealed record UserResponse
{
    public Guid Id { get; init; }
    public string Email { get; init; }
    public string FullName { get; init; }
    public string ImageUrl { get; init; }
}
