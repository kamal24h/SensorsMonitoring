using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data
{

    public class SensorDbContext : DbContext
    {
        public DbSet<Reading> Readings { get; set; }
        public DbSet<ProcessedStats> ProcessedStats { get; set; }

        public SensorDbContext(DbContextOptions<SensorDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Reading>(entity =>
            {
                entity.ToTable("Readings");

                entity.HasKey(x => new
                {
                    x.DeviceId,
                    x.Metric,
                    x.Timestamp,
                    x.Sequence
                });

                entity.Property(x => x.DeviceId)
                      .HasMaxLength(50);

                entity.Property(x => x.Metric)
                      .HasMaxLength(50);

                entity.Property(x => x.Value)
                      .HasPrecision(18, 6);
            });

            modelBuilder.Entity<ProcessedStats>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.TotalLines);
                entity.Property(e => e.ReadingsStored);
                entity.Property(e => e.DuplicatesRemoved);
                entity.Property(e => e.InvalidRecords);
                entity.Property(e => e.UpdatedAt);
            });
        }
    }
}
