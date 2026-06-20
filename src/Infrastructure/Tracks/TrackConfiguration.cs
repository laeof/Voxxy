using Domain.Tracks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Tracks;

internal sealed class TrackConfiguration : IEntityTypeConfiguration<Track>
{
    public void Configure(EntityTypeBuilder<Track> builder)
    {
        builder.HasKey(t => t.Id);

        builder.HasOne(t => t.Release)
            .WithMany(r => r.Tracks)
            .HasForeignKey(t => t.ReleaseId);

        builder.HasMany(t => t.Genres)
            .WithMany(g => g.Tracks);

        builder.HasMany(t => t.Moods)
            .WithMany(m => m.Tracks);

        builder.HasMany(t => t.Artists)
            .WithMany(a => a.Tracks);
    }
}
