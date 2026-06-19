using Domain.Moods;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Moods;

internal sealed class MoodConfiguration : IEntityTypeConfiguration<Mood>
{
    public void Configure(EntityTypeBuilder<Mood> builder)
    {
        builder.HasKey(t => t.Id);

        builder.HasIndex(t => t.Name)
            .IsUnique();

        builder.HasMany(t => t.Tracks)
            .WithMany(t => t.Moods);
    }
}