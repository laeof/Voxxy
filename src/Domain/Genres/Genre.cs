using Domain.ArtistReleases;
using Domain.Tracks;
using SharedKernel;

namespace Domain.Genres;

public sealed class Genre : Entity
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public List<Track> Tracks { get; set; } = new();

    public static Genre Create(string name)
    {
        var genre = new Genre
        {
            Id = Guid.NewGuid(),
            Name = name,
        };

        return genre;
    }
}