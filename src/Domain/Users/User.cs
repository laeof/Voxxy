using Domain.Artists;
using Domain.Playlists;
using Domain.Roles;
using Domain.Token;
using SharedKernel;

namespace Domain.Users;

public sealed class User : Entity
{
    public Guid Id { get; set; }
    public string Email { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string PasswordHash { get; set; }
    public string ImageKey { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<Role> Roles { get; set; } = new();
    public List<Playlist> Playlists { get; set; } = new();
    public List<RefreshToken> RefreshTokens { get; set; } = new();
    public List<Artist> Artists { get; set; } = new();
    public List<Artist> CreatedArtists { get; set; } = new();
    public List<Artist> UpdatedArtists { get; set; } = new();
    public static User Create(Guid userId, string email, string firstName, string lastName, string passwordHash, string imageKey, DateTime createdAt)
    {
        return new User
        {
            Id = userId,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            PasswordHash = passwordHash,
            ImageKey = imageKey,
            CreatedAt = createdAt
        };
    }

    public static User AssignRole(User user, Role role)
    {
        user.Roles.Add(role);
        return user;
    }
}
