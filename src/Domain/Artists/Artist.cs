using Domain.ArtistReleases;
using Domain.Tracks;
using Domain.Users;
using SharedKernel;

namespace Domain.Artists;

public sealed class Artist : Entity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; }
    public string ImageKey { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public User User { get; set; }
    public Guid CreatedByUserId { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public User CreatedByUser { get; set; }
    public User? UpdatedByUser { get; set; }
    public List<Track> Tracks { get; set; } = new();
    public List<Release> Releases { get; set; } = new();
    public static Artist Create(Guid userId, string name, string imageKey, DateTime createdAt, Guid createdByUserId)
    {
        var artistId = Guid.NewGuid();

        return new Artist
        {
            Id = artistId,
            UserId = userId,
            Name = name,
            ImageKey = imageKey.Replace("{id}", artistId.ToString()),
            CreatedAt = createdAt,
            CreatedByUserId = createdByUserId,
        };
    }
}