using Domain.Tracks;
using SharedKernel;

namespace Domain.Moods;

public sealed class Mood : Entity
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public List<Track> Tracks { get; set; } = new();

    public static Mood Create(string name)
    {
        var mood = new Mood
        {
            Id = Guid.NewGuid(),
            Name = name,
        };

        return mood;
    }
}