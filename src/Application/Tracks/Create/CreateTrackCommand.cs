using Application.Abstractions.Messaging;
using Application.Tracks.GetById;

namespace Application.Tracks.Create;

public sealed record CreateTrackCommand(string Name, Guid ArtistId, Guid AlbumId, int AlbumOrder) : ICommand<Guid>;