using ApiIncidentFastTrack.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApiIncidentFastTrack.Data.Configurations;

public class ResolutionTypeConfiguration : IEntityTypeConfiguration<ResolutionType>
{
    public void Configure(EntityTypeBuilder<ResolutionType> builder)
    {
        builder.HasKey(r => r.ResolutionTypeId);

        builder.Property(r => r.Name).IsRequired().HasMaxLength(100);
        builder.Property(r => r.Description).HasMaxLength(500);

        builder.HasOne(r => r.Company)
            .WithMany(c => c.ResolutionTypes)
            .HasForeignKey(r => r.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
