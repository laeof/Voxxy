using Application.Abstractions.Messaging;

namespace Application.Artists.OnAccount;

public sealed record GetArtistOnAccountQuery(Guid UserId) : IQuery<ForArtistResponse>;