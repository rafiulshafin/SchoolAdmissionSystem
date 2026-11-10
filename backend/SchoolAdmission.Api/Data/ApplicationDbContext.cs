using Microsoft.EntityFrameworkCore;
using SchoolAdmission.Api.Models;

namespace SchoolAdmission.Api.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<AdmissionClass> AdmissionClasses => Set<AdmissionClass>();

    public DbSet<AdmissionHold> AdmissionHolds => Set<AdmissionHold>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Admission Class configuration
        modelBuilder.Entity<AdmissionClass>(entity =>
        {
            entity.ToTable("Classes");

            entity.HasKey(c => c.Id);

            entity.Property(c => c.Name)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(c => c.TotalSeats)
                .IsRequired();

            entity.ToTable(t =>
                t.HasCheckConstraint(
                    "CK_Classes_TotalSeats",
                    "\"TotalSeats\" > 0"
                ));
        });

        // Admission Hold configuration
        modelBuilder.Entity<AdmissionHold>(entity =>
        {
            entity.ToTable("AdmissionHolds");

            entity.HasKey(h => h.Id);

            entity.Property(h => h.StudentId)
                .IsRequired()
                .HasMaxLength(17);

            entity.Property(h => h.Status)
                .HasConversion(
                    status => status.ToString().ToUpperInvariant(),
                    status => Enum.Parse<HoldStatus>(status, true)
                )
                .HasMaxLength(20);

            entity.HasOne(h => h.AdmissionClass)
                .WithMany(c => c.AdmissionHolds)
                .HasForeignKey(h => h.ClassId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(h => new
            {
                h.ClassId,
                h.Status,
                h.ExpiresAt
            });

            entity.HasIndex(h => new
            {
                h.StudentId,
                h.ClassId
            })
            .HasFilter("\"Status\" IN ('HELD', 'CONFIRMED')")
            .IsUnique();
        });
    }
}