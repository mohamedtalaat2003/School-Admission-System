using Microsoft.EntityFrameworkCore;
using SchoolAdmissionSystem.Api.Models.Entities;

namespace SchoolAdmissionSystem.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Class> Classes => Set<Class>();
    public DbSet<Hold> Holds => Set<Hold>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Class>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Id).HasMaxLength(64);
            entity.Property(c => c.Name).IsRequired();
            entity.Property(c => c.TotalSeats).IsRequired();
            entity.Property(c => c.Version).IsConcurrencyToken();
            entity.Property(c => c.CreatedAt).IsRequired();
        });

        modelBuilder.Entity<Hold>(entity =>
        {
            entity.HasKey(h => h.Id);
            entity.Property(h => h.Id).HasMaxLength(64);
            entity.Property(h => h.ClassId).HasMaxLength(64).IsRequired();
            entity.Property(h => h.StudentId).HasMaxLength(32).IsRequired();
            entity.Property(h => h.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.Property(h => h.ExpiresAt).IsRequired();
            entity.Property(h => h.CreatedAt).IsRequired();

            entity.HasOne(h => h.Class)
                .WithMany(c => c.Holds)
                .HasForeignKey(h => h.ClassId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(h => h.ClassId);
            entity.HasIndex(h => h.StudentId);
            entity.HasIndex(h => new { h.ClassId, h.Status, h.ExpiresAt });
            entity.HasIndex(h => new { h.StudentId, h.ClassId });
        });
    }
}
