using Domain.Albums;
using Domain.Playlists;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Albums;

internal sealed class AlbumConfiguration : IEntityTypeConfiguration<Album>
{
    public void Configure(EntityTypeBuilder<Album> builder)
    {
        builder.HasKey(t => t.Id);

        builder.HasMany(p => p.Tracks)
            .WithOne(p => p.Album)
            .HasForeignKey(p => p.AlbumId);

        builder.HasOne(a => a.Artist)
            .WithMany(ar => ar.Albums)
            .HasForeignKey(a => a.ArtistId);

        builder.HasOne(a => a.CreatedByUser)
            .WithMany(a => a.Albums)
            .HasForeignKey(a => a.CreatedBy);

        builder.HasOne(a => a.UpdatedByUser)
            .WithMany()
            .HasForeignKey(a => a.UpdatedBy);
    }
}
