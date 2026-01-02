using Application.Abstractions.Messaging;

namespace Application.Artists.GetById;

public sealed record GetArtistByIdQuery(Guid ArtistId) : IQuery<ArtistResponse>;