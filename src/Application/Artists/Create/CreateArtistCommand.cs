using Application.Abstractions.Messaging;

namespace Application.Artists.Create;

public sealed record CreateArtistCommand(Guid UserId, string Name) : ICommand<Guid>;