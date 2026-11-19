using ApiIncidentFastTrack.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApiIncidentFastTrack.Data.Configurations;

public class LocationConfiguration : IEntityTypeConfiguration<Location>
{
    public void Configure(EntityTypeBuilder<Location> builder)
    {
        builder.HasKey(l => l.LocationId);

        builder.Property(l => l.Name).IsRequired().HasMaxLength(200);

        builder.HasOne(l => l.Store)
            .WithMany(s => s.Locations)
            .HasForeignKey(l => l.StoreId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
