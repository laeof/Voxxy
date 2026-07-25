using Connect.Domain.Player;
using Connect.Domain.Queue;
using Connect.Shared;

namespace Connect.Application.Abstractions.Services;

public interface IPlayerSessionService
{
    Task<PlayerState> GetStateAsync(Guid userId);
    Task<PlayerState> CreateSessionAsync(Guid userId);
    Task<PlayerState> PlayAsync(Guid userId, PlayRequest request);
    Task<PlayerState> PauseAsync(Guid userId, PlayRequest request);
    Task<PlayerState> ChangePositionAsync(Guid userId, PositionRequest request);
    Task<PlayerState> ConnectToDeviceAsync(Guid userId, string connectionId);
    Task<PlayerState> ChangeVolumeAsync(Guid userId, int volumePercent);
}