using Domain.Albums;
using Domain.Tracks;
using Domain.Users;
using SharedKernel;

namespace Domain.Artists;

public sealed class Artist : Entity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; }
    public string ImageUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public User User { get; set; }
    public Guid CreatedByUserId { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public User CreatedByUser { get; set; }
    public User? UpdatedByUser { get; set; }
    public List<Track> Tracks { get; set; } = new();
    public List<Album> Albums { get; set; } = new();
}