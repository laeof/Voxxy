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
            .WithOne(t => t.Artist)
            .HasForeignKey(t => t.ArtistId);

        builder.HasMany(a => a.Albums)
            .WithOne(al => al.Artist)
            .HasForeignKey(al => al.ArtistId);

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
