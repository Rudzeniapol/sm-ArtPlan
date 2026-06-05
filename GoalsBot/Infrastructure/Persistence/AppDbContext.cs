using GoalsBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GoalsBot.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<DailyGoal> DailyGoals => Set<DailyGoal>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>();
    public DbSet<CalendarSync> CalendarSyncs => Set<CalendarSync>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
