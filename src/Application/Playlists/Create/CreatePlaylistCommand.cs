using Application.Abstractions.Messaging;

namespace Application.Playlists.Create;

public sealed record CreatePlaylistCommand(string Name) : ICommand<Guid>;