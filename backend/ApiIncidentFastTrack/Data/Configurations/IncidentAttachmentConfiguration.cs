using ApiIncidentFastTrack.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApiIncidentFastTrack.Data.Configurations;

public class IncidentAttachmentConfiguration : IEntityTypeConfiguration<IncidentAttachment>
{
    public void Configure(EntityTypeBuilder<IncidentAttachment> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.FileName).IsRequired().HasMaxLength(260);
        builder.Property(a => a.StoragePath).IsRequired().HasMaxLength(1000);
        builder.Property(a => a.ContentType).IsRequired().HasMaxLength(100);

        // Cascade with the incident (hard delete of an acknowledged duplicate).
        // The stored file itself must be removed by the service.
        builder.HasOne(a => a.Incident)
            .WithMany(i => i.Attachments)
            .HasForeignKey(a => a.IncidentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.UploadedByUser)
            .WithMany(u => u.Attachments)
            .HasForeignKey(a => a.UploadedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
