using Domain.Albums;
using Domain.Artists;
using Domain.Follows;
using Domain.Genres;
using Domain.Moods;
using Domain.Playlists;
using Domain.Token;
using Domain.Tracks;
using Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Application.Abstractions.Data;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<Playlist> Playlists { get; }
    DbSet<Artist> Artists { get; }
    DbSet<Album> Albums { get; }
    DbSet<Track> Tracks { get; }
    DbSet<Following> Followings { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<Genre> Genres { get; }
    DbSet<Mood> Moods { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
