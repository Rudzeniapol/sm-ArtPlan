using GoalsBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoalsBot.Infrastructure.Persistence.Configurations;

public sealed class CalendarSyncConfiguration : IEntityTypeConfiguration<CalendarSync>
{
    public void Configure(EntityTypeBuilder<CalendarSync> builder)
    {
        builder.ToTable("calendar_syncs");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.UserId).IsRequired();
        builder.Property(c => c.Date).IsRequired();
        builder.Property(c => c.GoogleEventId)
            .HasMaxLength(256)
            .IsRequired();
        builder.Property(c => c.LastSyncedAt).IsRequired();

        builder.HasOne(c => c.User)
            .WithMany(u => u.CalendarSyncs)
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => new { c.UserId, c.Date }).IsUnique();
    }
}
