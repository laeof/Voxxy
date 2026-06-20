using Application.Abstractions.Data;
using Domain.ArtistReleases;
using Domain.Artists;
using Domain.Genres;
using Domain.Moods;
using Domain.Tracks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;
using SharedKernel;

namespace Application.ArtistReleases.Create.DomainEventHandlers;

public sealed class CreateReleaseTracksDomainEventHandler : IDomainEventHandler<ReleaseDataCreatedDomainEvent>
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateReleaseTracksDomainEventHandler(
        IApplicationDbContext context,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _dateTimeProvider = dateTimeProvider;
    }
    public async Task Handle(ReleaseDataCreatedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var tracks = new List<Track>();

        List<Genre> genres = await _context.Genres.Where(g => domainEvent.Genres.Contains(g.Id)).ToListAsync(cancellationToken);
        List<Mood> moods = await _context.Moods.Where(m => domainEvent.Moods.Contains(m.Id)).ToListAsync(cancellationToken);
        List<Artist> artists = await _context.Artists.Where(a => domainEvent.Artists.Contains(a.Id)).ToListAsync(cancellationToken);

        foreach (CreateTrackDto track in domainEvent.Tracks)
        {
            var trackMetaData = TrackMetaData.Create(track.Title, domainEvent.ReleaseId, artists, genres, moods, track.Duration, track.Position, _dateTimeProvider.UtcNow);
            var trackEntity = Track.CreateTrackRelease(trackMetaData, track.AudioKeyAsset, track.ImageKeyAsset, track.AudioStream);

            tracks.Add(trackEntity);
        }

        await using IDbContextTransaction transaction = await _context.BeginTransactionAsync(cancellationToken);

        try
        {
            _context.Tracks.AddRange(tracks); //todo: bulk insert instead of addrange

            await _context.SaveChangesAsync(cancellationToken);
            await _context.CommitTransactionAsync(cancellationToken);
        }
        catch (Exception)
        {
            await _context.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}