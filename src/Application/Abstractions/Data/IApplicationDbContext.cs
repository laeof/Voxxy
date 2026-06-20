using Domain.ArtistReleases;
using Domain.Artists;
using Domain.Follows;
using Domain.Genres;
using Domain.Moods;
using Domain.OutboxMessages;
using Domain.Playlists;
using Domain.Token;
using Domain.Tracks;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Application.Abstractions.Data;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<Playlist> Playlists { get; }
    DbSet<Artist> Artists { get; }
    DbSet<Track> Tracks { get; }
    DbSet<Following> Followings { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<Genre> Genres { get; }
    DbSet<Mood> Moods { get; }
    DbSet<Release> Releases { get; }
    DbSet<OutboxMessage> OutboxMessages { get; }

    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

}
