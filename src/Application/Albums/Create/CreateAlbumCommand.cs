using Application.Abstractions.Messaging;

namespace Application.Albums.Create;

public sealed record CreateAlbumCommand(string Name) : ICommand<Guid>;