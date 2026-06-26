using Connect.Application.Abstractions.Repositories;
using Connect.Domain.Queue;
using Connect.Domain.Player;

namespace Connect.Application.Abstractions.Services;

public interface IQueuePlaybackService
{
    Task<QueuePlayback> CreateQueuePlaybackAsync(Guid userId);
    Task<QueuePlayback> GetQueuePlaybackAsync(Guid userId);
    Task<QueuePlayback> AddTracksAsync(Guid userId, List<QueueTrack> tracks);
    Task<QueuePlayback> ShuffleAsync(Guid userId);
    Task<QueuePlayback> UnshuffleAsync(Guid userId);
    Task<QueuePlayback> ToggleRepeatAsync(Guid userId, RepeatingState state);
    Task<QueuePlayback> RemoveTrackAsync(Guid userId, Guid trackId);
    Task<QueuePlayback> ChangeTrackIndexAsync(Guid userId, Guid trackId, int newIndex);
}