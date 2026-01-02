using SharedKernel;
using SharedKernel.Enums;

namespace Domain.Playlists;

public sealed record PlaylistCreatedDomainEvent(Guid PlaylistId, Guid UserId, PlaylistType Type) : IDomainEvent;