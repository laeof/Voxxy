using Domain.Artists;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Artists;

internal sealed class ArtistConfiguration : IEntityTypeConfiguration<Artist>
{
    public void Configure(EntityTypeBuilder<Artist> builder)
    {
        builder.HasKey(t => t.Id);

        builder.HasMany(a => a.Tracks)
            .WithOne(t => t.Artist)
            .HasForeignKey(t => t.ArtistId);

        builder.HasMany(a => a.Albums)
            .WithOne(al => al.Artist)
            .HasForeignKey(al => al.ArtistId);
    }
}
