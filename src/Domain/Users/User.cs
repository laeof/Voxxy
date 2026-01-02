using Domain.Albums;
using Domain.Playlists;
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
    public string ImageUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<Playlist> Playlists { get; set; } = new();
    public List<RefreshToken> RefreshTokens { get; set; } = new();
    public List<Album> Albums { get; set; } = new();
}
