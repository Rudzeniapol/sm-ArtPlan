using GoalsBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoalsBot.Infrastructure.Persistence.Configurations;

public sealed class DailyGoalConfiguration : IEntityTypeConfiguration<DailyGoal>
{
    public void Configure(EntityTypeBuilder<DailyGoal> builder)
    {
        builder.ToTable("daily_goals");

        builder.HasKey(g => g.Id);

        builder.Property(g => g.UserId).IsRequired();
        builder.Property(g => g.Date).IsRequired();
        builder.Property(g => g.RawInput).IsRequired();
        builder.Property(g => g.CreatedAt).IsRequired();

        builder.HasOne(g => g.User)
            .WithMany(u => u.DailyGoals)
            .HasForeignKey(g => g.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(g => new { g.UserId, g.Date }).IsUnique();
    }
}
