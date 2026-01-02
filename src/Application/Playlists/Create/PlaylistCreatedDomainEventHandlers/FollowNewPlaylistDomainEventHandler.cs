using Application.Abstractions.Data;
using Domain.Follows;
using Domain.Playlists;
using SharedKernel;
using SharedKernel.Enums;

namespace Application.Playlists.Create.PlaylistCreatedDomainEventHandlers;

internal sealed class FollowNewPlaylistDomainEventHandler : IDomainEventHandler<PlaylistCreatedDomainEvent>
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTimeProvider _dateTimeProvider;

    public FollowNewPlaylistDomainEventHandler(
        IApplicationDbContext context,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task Handle(PlaylistCreatedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var follow = new Following
        {
            FollowerId = domainEvent.UserId,
            FolloweeId = domainEvent.PlaylistId,
            Type = (FollowType)domainEvent.Type,
            CreatedAt = _dateTimeProvider.UtcNow,
        };

        _context.Followings.Add(follow);

        await _context.SaveChangesAsync(cancellationToken);
    }
}