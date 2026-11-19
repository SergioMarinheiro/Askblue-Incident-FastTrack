using ApiIncidentFastTrack.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApiIncidentFastTrack.Data.Configurations;

public class IncidentCommentConfiguration : IEntityTypeConfiguration<IncidentComment>
{
    public void Configure(EntityTypeBuilder<IncidentComment> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Content).IsRequired().HasMaxLength(4000);

        // Cascade: when an incident is hard-deleted (duplicate acknowledged at submit),
        // its comments go with it.
        builder.HasOne(c => c.Incident)
            .WithMany(i => i.Comments)
            .HasForeignKey(c => c.IncidentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.User)
            .WithMany(u => u.Comments)
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
