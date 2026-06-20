using Domain.Artists;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Artists;

internal sealed class ArtistConfiguration : IEntityTypeConfiguration<Artist>
{
    public void Configure(EntityTypeBuilder<Artist> builder)
    {
        builder.HasKey(t => t.Id);

        builder.HasIndex(t => t.Name)
            .IsUnique();

        builder.HasMany(a => a.Tracks)
            .WithMany(t => t.Artists);

        builder.HasMany(a => a.Releases)
            .WithMany(al => al.Artists);

        builder.HasOne(a => a.User)
            .WithMany(u => u.Artists)
            .HasForeignKey(a => a.UserId);

        builder.HasOne(a => a.CreatedByUser)
            .WithMany(u => u.CreatedArtists)
            .HasForeignKey(a => a.CreatedByUserId);

        builder.HasOne(a => a.UpdatedByUser)
            .WithMany(u => u.UpdatedArtists)
            .HasForeignKey(a => a.UpdatedByUserId);
    }
}
