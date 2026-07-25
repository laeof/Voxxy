using Connect.Application.Abstractions.Repositories;
using Connect.Application.Abstractions.Services;
using Connect.Domain.Player;
using Connect.Shared;

namespace Connect.Application.Player;

public sealed class PlayerSessionService : IPlayerSessionService
{
    private readonly IPlayerSessionRepository _repository;

    public PlayerSessionService(IPlayerSessionRepository repository)
    {
        _repository = repository;
    }

    public async Task<PlayerState> GetStateAsync(Guid userId)
    {
        PlayerState? state = await _repository.GetValueAsync(userId);

        return state;
    }

    public async Task<PlayerState> PlayAsync(Guid userId, PlayRequest request)
    {
        PlayerState state = await GetStateAsync(userId);

        state.Play(request.TrackId, request.QueueId, request.PositionMs, request.UpdatedAt);

        await _repository.SetValueAsync(userId, state);

        return state;
    }

    public async Task<PlayerState> PauseAsync(Guid userId, PlayRequest request)
    {
        PlayerState state = await GetStateAsync(userId);

        state.Stop(request.TrackId, request.QueueId, request.PositionMs, request.UpdatedAt);

        await _repository.SetValueAsync(userId, state);

        return state;
    }

    public async Task<PlayerState> ChangePositionAsync(Guid userId, PositionRequest request)
    {
        PlayerState state = await GetStateAsync(userId);

        state.ChangePosition(request.PositionMs, request.UpdatedAt);

        await _repository.SetValueAsync(userId, state);

        return state;
    }

    public async Task<PlayerState> ConnectToDeviceAsync(Guid userId, string connectionId)
    {
        PlayerState state = await GetStateAsync(userId);

        state.ConnectToDevice(connectionId);

        await _repository.SetValueAsync(userId, state);

        return state;
    }

    public async Task<PlayerState> ChangeVolumeAsync(Guid userId, int volumePercent)
    {
        PlayerState state = await GetStateAsync(userId);

        state.ChangeVolume(volumePercent);

        await _repository.SetValueAsync(userId, state);

        return state;
    }

    public async Task<PlayerState> CreateSessionAsync(Guid userId)
    {
        var state = new PlayerState(userId);
        await _repository.SetValueAsync(userId, state);
        return state;
    }

}