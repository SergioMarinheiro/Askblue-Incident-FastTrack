using ApiIncidentFastTrack.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApiIncidentFastTrack.Data.Configurations;

public class IncidentConfiguration : IEntityTypeConfiguration<Incident>
{
    public void Configure(EntityTypeBuilder<Incident> builder)
    {
        builder.HasKey(i => i.Id);

        // Only CustomerName is required at the database level (drafts).
        // "Required on submit" is enforced in the service, not here.
        builder.Property(i => i.CustomerName).IsRequired().HasMaxLength(200);
        builder.Property(i => i.Title).HasMaxLength(200);
        builder.Property(i => i.Description).HasMaxLength(4000);
        builder.Property(i => i.CustomerAddress).HasMaxLength(500);
        builder.Property(i => i.CustomerEmail).HasMaxLength(256);
        builder.Property(i => i.CustomerPhone).HasMaxLength(50);
        builder.Property(i => i.RejectionReason).HasMaxLength(1000);

        // Money: 18 digits, 2 decimals. Without this EF warns and SQL Server may truncate.
        builder.Property(i => i.EstimatedCompensationAmount).HasPrecision(18, 2);

        // Enums (Status, Priority, IncidentType) are stored as int, the EF default.

        // Most lists filter by company and status
        builder.HasIndex(i => new { i.CompanyId, i.Status });

        // All Restrict: incidents are never deleted by cascading from a parent.
        // (SQL Server would also reject several cascade paths into one table.)
        builder.HasOne(i => i.Company)
            .WithMany(c => c.Incidents)
            .HasForeignKey(i => i.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.CreatedByUser)
            .WithMany(u => u.CreatedIncidents)
            .HasForeignKey(i => i.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.ClosedByUser)
            .WithMany(u => u.ClosedIncidents)
            .HasForeignKey(i => i.ClosedByUserId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.ApprovedByUser)
            .WithMany(u => u.ApprovedIncidents)
            .HasForeignKey(i => i.ApprovedByUserId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.Store)
            .WithMany(s => s.Incidents)
            .HasForeignKey(i => i.StoreId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.Location)
            .WithMany(l => l.Incidents)
            .HasForeignKey(i => i.LocationId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.ResolutionType)
            .WithMany(r => r.Incidents)
            .HasForeignKey(i => i.ResolutionTypeId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
