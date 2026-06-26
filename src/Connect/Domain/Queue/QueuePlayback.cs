using Connect.Domain.Player;

namespace Connect.Domain.Queue;

public sealed class QueuePlayback
{
    public Guid QueueId { get; set; }
    public List<QueueTrack> Tracks { get; set; }
    public List<QueueTrack> TracksWithoutShuffling { get; set; }
    public bool IsShuffling { get; set; }
    public RepeatingState RepeatingState { get; set; }

    public QueuePlayback(Guid queueId)
    {
        QueueId = queueId;
        Tracks = [];
        TracksWithoutShuffling = [];
        IsShuffling = false;
        RepeatingState = RepeatingState.None;
    }

    public void AddTracks(List<QueueTrack> track)
    {
        Tracks.AddRange(track);
    }

    public void Shuffle()
    {
        TracksWithoutShuffling = [.. Tracks];
        Tracks = [.. Tracks.Shuffle()];
        IsShuffling = true;
    }

    public void Unshuffle()
    {
        if (TracksWithoutShuffling.Count > 0)
        {
            Tracks = [.. TracksWithoutShuffling];
            TracksWithoutShuffling.Clear();
        }

        IsShuffling = false;
    }

    public void ToggleRepeat(RepeatingState state)
    {
        RepeatingState = state;
    }

    public void RemoveTrack(Guid trackId)
    {
        Tracks.RemoveAll(t => t.TrackId == trackId);
        TracksWithoutShuffling.RemoveAll(t => t.TrackId == trackId);
    }

    public void ChangeTrackIndex(Guid trackId, int newIndex)
    {
        QueueTrack track = Tracks.FirstOrDefault(t => t.TrackId == trackId);

        if (track != null)
        {
            Tracks.Remove(track);
            Tracks.Insert(newIndex, track);
        }
    }
}