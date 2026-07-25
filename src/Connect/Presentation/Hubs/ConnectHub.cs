using Connect.Application.Abstractions;
using Connect.Application.Abstractions.Services;
using Connect.Domain.Devices;
using Connect.Domain.Player;
using Connect.Domain.Queue;
using Connect.Infrastructure.Redis;
using Connect.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.SignalR;

namespace Connect.Presentation.Hubs;

[Authorize]
public sealed class PlayerHub : Hub<IPlayerClient>
{
    private readonly IPlayerSessionService _playerSessionService;
    private readonly IQueuePlaybackService _queuePlaybackService;
    private readonly IDeviceService _deviceService;

    public PlayerHub(
        IPlayerSessionService playerSessionService,
        IQueuePlaybackService queuePlaybackService,
        IDeviceService deviceService)
    {
        _playerSessionService = playerSessionService;
        _queuePlaybackService = queuePlaybackService;
        _deviceService = deviceService;
    }

    public override async Task OnConnectedAsync()
    {
        string userId = Context.UserIdentifier!;

        await Groups.AddToGroupAsync(Context.ConnectionId, $"{TableConstants.PlayerSessionTable}:{userId}");
        await Groups.AddToGroupAsync(Context.ConnectionId, $"{TableConstants.QueuePlaybackTable}:{userId}");
        await Groups.AddToGroupAsync(Context.ConnectionId, $"{TableConstants.DeviceTable}:{userId}");
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        string userId = Context.UserIdentifier!;

        await _deviceService.DisconnectDeviceAsync(Guid.Parse(userId), Context.ConnectionId);

        Device device = await _deviceService.GetOnlineDevicesAsync(Guid.Parse(userId));

        await ConnectToDeviceOnDisconnect(Guid.Parse(userId), device);

        await Clients.Group($"{TableConstants.DeviceTable}:{userId}").DeviceListChanged(device);

        await base.OnDisconnectedAsync(exception);
    }

    public async Task RegisterDevice(DeviceItem deviceItem)
    {
        if (deviceItem is null)
        {
            throw new HubException("Device item is required");
        }

        var userId = Guid.Parse(Context.UserIdentifier!);

        Device device = await _deviceService.GetDevicesAsync(userId);

        if (device is null || !device.Items.Any(d => d.Id == deviceItem.Id))
        {
            await _deviceService.AddDeviceAsync(userId, deviceItem);
        }

        await _deviceService.ConnectDeviceAsync(userId, deviceItem.Id, Context.ConnectionId);

        device = await _deviceService.GetOnlineDevicesAsync(userId);

        PlayerState state = await _playerSessionService.GetStateAsync(userId);

        if (string.IsNullOrEmpty(state.ActiveDeviceId))
        {
            await ConnectDeviceToPlayer(userId, Context.ConnectionId);
            state = await _playerSessionService.GetStateAsync(userId);
        }

        await Clients.Group($"{TableConstants.DeviceTable}:{userId}").DeviceListChanged(device);
        await Clients.Group($"{TableConstants.PlayerSessionTable}:{userId}").ActiveDeviceChanged(state.ActiveDeviceId!);
    }

    public async Task RegisterPlayer()
    {
        var userId = Guid.Parse(Context.UserIdentifier!);

        PlayerState state = await _playerSessionService.GetStateAsync(userId);

        state ??= await _playerSessionService.CreateSessionAsync(userId);

        await Clients.Group($"{TableConstants.PlayerSessionTable}:{userId}").PlayerStateChanged(state);
    }

    public async Task RegisterQueue()
    {
        var userId = Guid.Parse(Context.UserIdentifier!);

        QueuePlayback queue = await _queuePlaybackService.GetQueuePlaybackAsync(userId);

        queue ??= await _queuePlaybackService.CreateQueuePlaybackAsync(userId);

        await Clients.Caller.QueuePlaybackChanged(queue);
    }

    public async Task Play(PlayRequest request)
    {
        var userId = Guid.Parse(Context.UserIdentifier!);

        PlayerState state = await _playerSessionService.PlayAsync(userId, request);

        await Clients.Group($"{TableConstants.PlayerSessionTable}:{userId}").PlayerStateChanged(state);
    }

    public async Task Pause(PlayRequest request)
    {
        var userId = Guid.Parse(Context.UserIdentifier!);

        PlayerState state = await _playerSessionService.PauseAsync(userId, request);

        await Clients.Group($"{TableConstants.PlayerSessionTable}:{userId}").PlayerStateChanged(state);
    }

    public async Task ChangePosition(PositionRequest request)
    {
        string userId = Context.UserIdentifier!;

        PlayerState state = await _playerSessionService.ChangePositionAsync(Guid.Parse(userId), request);

        await Clients.Group($"{TableConstants.PlayerSessionTable}:{userId}").PositionChanged(state.PositionMs, state.UpdatedAt);
    }

    public async Task ConnectToDevice(string connectionId)
    {
        string userId = Context.UserIdentifier!;

        await _playerSessionService.ConnectToDeviceAsync(Guid.Parse(userId), connectionId);

        await Clients.Group($"{TableConstants.PlayerSessionTable}:{userId}").ActiveDeviceChanged(connectionId);
    }

    public async Task ChangeVolume(int volume)
    {
        string userId = Context.UserIdentifier!;

        PlayerState state = await _playerSessionService.ChangeVolumeAsync(Guid.Parse(userId), volume);

        await Clients.Group($"{TableConstants.PlayerSessionTable}:{userId}").VolumeChanged(state.VolumePercent);
    }

    public async Task AddTracksToQueue(List<QueueTrack> tracks)
    {
        string userId = Context.UserIdentifier!;

        QueuePlayback queue = await _queuePlaybackService.AddTracksAsync(Guid.Parse(userId), tracks);

        await Clients.Group($"{TableConstants.QueuePlaybackTable}:{userId}").QueuePlaybackChanged(queue);
    }

    public async Task RemoveTrackFromQueue(Guid trackId)
    {
        string userId = Context.UserIdentifier!;

        QueuePlayback queue = await _queuePlaybackService.RemoveTrackAsync(Guid.Parse(userId), trackId);

        await Clients.Group($"{TableConstants.QueuePlaybackTable}:{userId}").QueuePlaybackChanged(queue);
    }

    public async Task ChangeTrackIndexInQueue(Guid trackId, int newIndex)
    {
        string userId = Context.UserIdentifier!;

        QueuePlayback queue = await _queuePlaybackService.ChangeTrackIndexAsync(Guid.Parse(userId), trackId, newIndex);

        await Clients.Group($"{TableConstants.QueuePlaybackTable}:{userId}").QueuePlaybackChanged(queue);
    }

    public async Task ShuffleQueue()
    {
        string userId = Context.UserIdentifier!;

        QueuePlayback queue = await _queuePlaybackService.ShuffleAsync(Guid.Parse(userId));

        await Clients.Group($"{TableConstants.QueuePlaybackTable}:{userId}").QueuePlaybackChanged(queue);
    }

    public async Task UnshuffleQueue()
    {
        string userId = Context.UserIdentifier!;

        QueuePlayback queue = await _queuePlaybackService.UnshuffleAsync(Guid.Parse(userId));

        await Clients.Group($"{TableConstants.QueuePlaybackTable}:{userId}").QueuePlaybackChanged(queue);
    }

    public async Task ToggleRepeatQueue(RepeatingState state)
    {
        string userId = Context.UserIdentifier!;

        QueuePlayback queue = await _queuePlaybackService.ToggleRepeatAsync(Guid.Parse(userId), state);

        await Clients.Group($"{TableConstants.QueuePlaybackTable}:{userId}").QueuePlaybackChanged(queue);
    }

    private async Task ConnectToDeviceOnDisconnect(Guid userId, Device device)
    {
        PlayerState state = await _playerSessionService.GetStateAsync(userId);

        if (state is not null && state.ActiveDeviceId == Context.ConnectionId)
        {
            string connectionId = device.Items.Count > 0 ? device.Items[0].ConnectionId! : string.Empty;

            await ConnectDeviceToPlayer(userId, connectionId);
        }
    }

    private async Task ConnectDeviceToPlayer(Guid userId, string connectionId)
    {
        await _playerSessionService.ConnectToDeviceAsync(userId, connectionId);
        await Clients.Group($"{TableConstants.PlayerSessionTable}:{userId}").ActiveDeviceChanged(connectionId);
    }
}