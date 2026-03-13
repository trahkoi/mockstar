using Microsoft.EntityFrameworkCore;
using Mockstar.Web.Persistence.Entities;

namespace Mockstar.Web.Persistence;

public class HeatDbContext : DbContext
{
    public HeatDbContext(DbContextOptions<HeatDbContext> options) : base(options)
    {
    }

    public DbSet<EventEntity> Events => Set<EventEntity>();
    public DbSet<HeatEntity> Heats => Set<HeatEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EventEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(256);
            entity.Property(e => e.Name).HasMaxLength(512);

            entity.HasMany(e => e.Heats)
                .WithOne(h => h.Event)
                .HasForeignKey(h => h.EventId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<HeatEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.HeatId).HasMaxLength(256);
            entity.Property(e => e.Name).HasMaxLength(256);
            entity.Property(e => e.HeatType).HasMaxLength(64);
            entity.Property(e => e.EventId).HasMaxLength(256);

            entity.HasIndex(e => e.EventId);
        });
    }
}
