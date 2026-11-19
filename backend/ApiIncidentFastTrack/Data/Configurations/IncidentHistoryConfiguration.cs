using ApiIncidentFastTrack.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApiIncidentFastTrack.Data.Configurations;

public class IncidentHistoryConfiguration : IEntityTypeConfiguration<IncidentHistory>
{
    public void Configure(EntityTypeBuilder<IncidentHistory> builder)
    {
        builder.HasKey(h => h.IncidentHistoryId);

        builder.Property(h => h.FieldName).IsRequired().HasMaxLength(100);
        builder.Property(h => h.OldValue).HasMaxLength(4000);
        builder.Property(h => h.NewValue).HasMaxLength(4000);

        // All rows written in one save share a ChangeSetId
        builder.HasIndex(h => h.ChangeSetId);

        builder.HasOne(h => h.Incident)
            .WithMany(i => i.History)
            .HasForeignKey(h => h.IncidentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(h => h.ChangedByUser)
            .WithMany(u => u.IncidentHistoryEntries)
            .HasForeignKey(h => h.ChangedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
