using ApiIncidentFastTrack.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApiIncidentFastTrack.Data.Configurations;

public class IncidentDuplicateConfiguration : IEntityTypeConfiguration<IncidentDuplicate>
{
    public void Configure(EntityTypeBuilder<IncidentDuplicate> builder)
    {
        builder.HasKey(d => d.Id);

        builder.Property(d => d.DetectionScore).HasPrecision(5, 2);
        builder.Property(d => d.Justification).HasMaxLength(1000);

        // One flag per pair of incidents
        builder.HasIndex(d => new { d.IncidentId, d.PotentialDuplicateIncidentId }).IsUnique();

        // The flagged incident: its flags are removed with it.
        builder.HasOne(d => d.Incident)
            .WithMany(i => i.DuplicateRelationships)
            .HasForeignKey(d => d.IncidentId)
            .OnDelete(DeleteBehavior.Cascade);

        // The matched incident: Restrict, because SQL Server does not allow two cascade
        // paths from Incidents into this table. Before hard-deleting an incident, the
        // service must remove any flags where it is the matched incident.
        builder.HasOne(d => d.PotentialDuplicateIncident)
            .WithMany(i => i.PotentialDuplicateOfRelationships)
            .HasForeignKey(d => d.PotentialDuplicateIncidentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.ReviewedByUser)
            .WithMany(u => u.ReviewedDuplicates)
            .HasForeignKey(d => d.ReviewedByUserId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
