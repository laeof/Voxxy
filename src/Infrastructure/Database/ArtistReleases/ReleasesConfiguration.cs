using Domain.ArtistReleases;
using Domain.Playlists;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.ArtistReleases;

internal sealed class ReleaseConfiguration : IEntityTypeConfiguration<Release>
{
    public void Configure(EntityTypeBuilder<Release> builder)
    {
        builder.HasKey(t => t.Id);

        builder.HasMany(p => p.Tracks)
            .WithOne(p => p.Release)
            .HasForeignKey(p => p.ReleaseId);

        builder.HasMany(a => a.Artists)
            .WithMany(ar => ar.Releases);
    }
}
