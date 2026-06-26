using Connect.Application.Abstractions.Repositories;
using Connect.Application.Abstractions.Services;
using Connect.Domain.Player;
using Connect.Domain.Queue;

namespace Application.QueuePlaybacks;

public class QueuePlaybackService : IQueuePlaybackService
{
    private readonly IQueuePlaybackRepository _queuePlaybackRepository;

    public QueuePlaybackService(IQueuePlaybackRepository queuePlaybackRepository)
    {
        _queuePlaybackRepository = queuePlaybackRepository;
    }

    public async Task<QueuePlayback> AddTracksAsync(Guid userId, List<QueueTrack> tracks)
    {
        QueuePlayback queuePlayback = await _queuePlaybackRepository.GetValueAsync(userId);
        queuePlayback.AddTracks(tracks);
        await _queuePlaybackRepository.SetValueAsync(userId, queuePlayback);
        return queuePlayback;
    }

    public async Task<QueuePlayback> ChangeTrackIndexAsync(Guid userId, Guid trackId, int newIndex)
    {
        QueuePlayback queuePlayback = await _queuePlaybackRepository.GetValueAsync(userId);
        queuePlayback.ChangeTrackIndex(trackId, newIndex);
        await _queuePlaybackRepository.SetValueAsync(userId, queuePlayback);
        return queuePlayback;
    }

    public async Task<QueuePlayback> CreateQueuePlaybackAsync(Guid userId)
    {
        var queuePlayback = new QueuePlayback(userId);
        await _queuePlaybackRepository.SetValueAsync(userId, queuePlayback);
        return queuePlayback;
    }

    public async Task<QueuePlayback> GetQueuePlaybackAsync(Guid userId)
    {
        QueuePlayback queuePlayback = await _queuePlaybackRepository.GetValueAsync(userId);
        return queuePlayback;
    }

    public async Task<QueuePlayback> RemoveTrackAsync(Guid userId, Guid trackId)
    {
        QueuePlayback queuePlayback = await _queuePlaybackRepository.GetValueAsync(userId);
        queuePlayback.RemoveTrack(trackId);
        await _queuePlaybackRepository.SetValueAsync(userId, queuePlayback);
        return queuePlayback;
    }

    public async Task<QueuePlayback> ShuffleAsync(Guid userId)
    {
        QueuePlayback queuePlayback = await _queuePlaybackRepository.GetValueAsync(userId);
        queuePlayback.Shuffle();
        await _queuePlaybackRepository.SetValueAsync(userId, queuePlayback);
        return queuePlayback;
    }

    public async Task<QueuePlayback> ToggleRepeatAsync(Guid userId, RepeatingState state)
    {
        QueuePlayback queuePlayback = await _queuePlaybackRepository.GetValueAsync(userId);
        queuePlayback.ToggleRepeat(state);
        await _queuePlaybackRepository.SetValueAsync(userId, queuePlayback);
        return queuePlayback;
    }

    public async Task<QueuePlayback> UnshuffleAsync(Guid userId)
    {
        QueuePlayback queuePlayback = await _queuePlaybackRepository.GetValueAsync(userId);
        queuePlayback.Unshuffle();
        await _queuePlaybackRepository.SetValueAsync(userId, queuePlayback);
        return queuePlayback;
    }

}