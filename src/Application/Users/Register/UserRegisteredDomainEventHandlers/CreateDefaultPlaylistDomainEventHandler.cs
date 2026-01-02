using Application.Abstractions.Data;
using Domain.Playlists;
using Domain.Users;
using SharedKernel;
using SharedKernel.Constants;
using SharedKernel.Enums;

namespace Application.Users.Register.UserRegisteredDomainEventHandlers;

internal sealed class CreateDefaultPlaylistDomainEventHandler : IDomainEventHandler<UserRegisteredDomainEvent>
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateDefaultPlaylistDomainEventHandler(
        IApplicationDbContext context,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task Handle(UserRegisteredDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var playlist = Playlist.CreateDefault(domainEvent.UserId, _dateTimeProvider);

        _context.Playlists.Add(playlist);

        await _context.SaveChangesAsync(cancellationToken);
    }
}
