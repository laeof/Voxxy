using Application.Abstractions.Messaging;

namespace Application.Albums.GetById;

public sealed record GetAlbumByIdQuery(Guid AlbumId) : IQuery<AlbumResponse>;